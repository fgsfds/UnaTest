using System.Text.Json;
using Broker.Messages;
using Core;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Broker;

public sealed class ConsumerFactory<T> where T : IMessage
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILoggerFactory _loggerFactory;

    public ConsumerFactory(
        ConnectionManager connectionManager, 
        ILoggerFactory loggerFactory
        )
    {
        _connectionManager = connectionManager;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Создает потребителя.
    /// </summary>
    /// <param name="queue">Название очереди.</param>
    public Consumer<T> Create(string queue) => new(queue, _connectionManager, _loggerFactory.CreateLogger<Consumer<T>>());
}


/// <summary>
/// Абстракция над потребителем сообщений.
/// </summary>
public sealed class Consumer<T> : IAsyncDisposable where T: IMessage
{
    public EventHandler<MessageWrapper<T>>? MessageReceived;

    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<Consumer<T>> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly string _queue;

    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;

    public Consumer(
        string queue,
        ConnectionManager connectionManager,
        ILogger<Consumer<T>> logger
        )
    {
        _queue = queue;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Начинает получение сообщений из очереди.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;

        _channel = await _connectionManager.GetChannelAsync();

        await _channel.BasicQosAsync(0, 5, false, linkedToken);

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(_queue, false, _consumer, linkedToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, linkedToken);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();

        _consumer?.ReceivedAsync -= OnMessageReceived;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
    }

    public ValueTask NackWithoutRequeue(ulong deliveryTag)
    {
        ArgumentNullException.ThrowIfNull(_channel);
        return _channel.BasicNackAsync(deliveryTag, false, false);
    }

    public ValueTask Ack(ulong deliveryTag)
    {
        ArgumentNullException.ThrowIfNull(_channel);
        return _channel.BasicAckAsync(deliveryTag, false);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        try
        {
            var message = JsonSerializer.Deserialize<T>(args.Body.ToArray());

            if (message is null)
            {
                _logger.LogError("Ошибка при десериализации сообщения.");
                await _channel.BasicNackAsync(args.DeliveryTag, false, false);
                return;
            }

            MessageReceived?.Invoke(this, new() { DeliveryTag = args.DeliveryTag, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения.");
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
    }
}
