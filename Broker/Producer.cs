using System.Text.Json;
using Broker.Messages;
using Core;
using RabbitMQ.Client;

namespace Broker;

/// <summary>
/// Абстракция над продюсером сообщений.
/// </summary>
public sealed class Producer
{
    private static readonly BasicProperties _properties = new() { Persistent = true };
    private readonly ConnectionManager _connectionManager;

    public Producer(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Отправить типизированное сообщение брокеру.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public async Task SendAsync(UploadedFileMessage message)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await using var channel = await _connectionManager.GetChannelAsync();

        await channel.BasicPublishAsync(
            Consts.ExchangeTopicAmq,
            Consts.RoutingKeyPdfUploaded,
            true,
            _properties,
            body
            );
    }
}
