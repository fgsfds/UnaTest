using System.Text;
using System.Text.Json;
using Core;
using Core.Messages;
using Database;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UglyToad.PdfPig;

namespace PdfWorker;

/// <summary>
/// Сервис обработки входящих PDF.
/// </summary>
public class IncomingPdfsService : BackgroundService
{
    private readonly IDbContextFactory<DatabaseContext> _dbFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<IncomingPdfsService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;

    public IncomingPdfsService(
        IDbContextFactory<DatabaseContext> dbFactory,
        IHttpClientFactory httpFactory,
        ConnectionFactory connectionFactory,
        ILogger<IncomingPdfsService> logger
        )
    {
        _dbFactory = dbFactory;
        _httpFactory = httpFactory;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await _channel.BasicQosAsync(0, 5, false, cancellationToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(Consts.QueuePdfIncoming, false, _consumer, cancellationToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.ReceivedAsync -= OnMessageReceived;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();

        }

        await base.StopAsync(cancellationToken);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        try
        {
            var message = JsonSerializer.Deserialize<UploadedFileMessage>(args.Body.ToArray());

            if (message is null)
            {
                _logger.LogError("Ошибка при десериализации сообщения.");
                await _channel.BasicNackAsync(args.DeliveryTag, false, false);
                return;
            }

            using var httpClient = _httpFactory.CreateClient();
            using var requestResponse = await httpClient.GetAsync(message.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!requestResponse.IsSuccessStatusCode)
            {
                //TODO retry
                _logger.LogError($"Ошибка при скачивании файла {message.DownloadUrl}.");
                await _channel.BasicNackAsync(args.DeliveryTag, false, false);
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
                FileName = message.FileName,
                Content = fullText.ToString()
            });

            await dbContext.SaveChangesAsync();

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения.");
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
    }
}
