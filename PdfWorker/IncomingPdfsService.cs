using System.Text;
using Broker;
using Broker.Messages;
using Core;
using Database;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace PdfWorker;

/// <summary>
/// Сервис обработки входящих PDF.
/// </summary>
public class IncomingPdfsService : BackgroundService
{
    private readonly IDbContextFactory<DatabaseContext> _dbFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<IncomingPdfsService> _logger;
    private readonly Consumer<UploadedFileMessage> _consumer;

    public IncomingPdfsService(
        ConsumerFactory<UploadedFileMessage> consumerFactory,
        IDbContextFactory<DatabaseContext> dbFactory,
        IHttpClientFactory httpFactory,
        ILogger<IncomingPdfsService> logger
        )
    {
        _consumer = consumerFactory.Create(Consts.QueuePdfIncoming);
        _dbFactory = dbFactory;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _consumer.MessageReceived += OnMessageReceived;
        await _consumer.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private async void OnMessageReceived(object? sender, MessageWrapper<UploadedFileMessage> message)
    {
        if (sender is not Consumer<UploadedFileMessage> consumer)
        {
            throw new InvalidCastException();
        }

        try
        {
            using var httpClient = _httpFactory.CreateClient();
            using var requestResponse = await httpClient.GetAsync(message.Message.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!requestResponse.IsSuccessStatusCode)
            {
                //TODO retry
                _logger.LogError($"Ошибка при скачивании файла {message.Message.DownloadUrl}.");
                await _consumer.NackWithoutRequeue(message.DeliveryTag);
                return;
            }

            using var stream = await requestResponse.Content.ReadAsStreamAsync();
            using var document = PdfDocument.Open(stream);

            var fullText = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                fullText.Append(page.Text);
            }

            using var dbContext = await _dbFactory.CreateDbContextAsync();

            await dbContext.Pdfs.AddAsync(new()
            {
                FileName = message.Message.FileName,
                Content = fullText.ToString()
            });

            await dbContext.SaveChangesAsync();

            await _consumer.Ack(message.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения.");
            await _consumer.NackWithoutRequeue(message.DeliveryTag);
        }
    }
}
