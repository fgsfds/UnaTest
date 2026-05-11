using Core.Config;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api;

/// <summary>
/// Менеджер подключений к RabbitMQ.
/// </summary>
public sealed class RabbitConnectionManager : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _semaphore = new(1);

    private IConnection? _connection;

    public RabbitConnectionManager(IOptions<CoreConfig> config)
    {
        _factory = new ConnectionFactory
        {
            HostName = config.Value.RabbitAddress,
            Port = config.Value.RabbitPort
        };
    }

    /// <summary>
    /// Возвращает IChannel.
    /// </summary>
    public async ValueTask<IChannel> GetChannelAsync()
    {
        await InitializeConnectionIfNeededAsync();
        return await _connection!.CreateChannelAsync();
    }

    private async ValueTask InitializeConnectionIfNeededAsync()
    {
        if (_connection?.IsOpen is true)
        {
            return;
        }

        await _semaphore.WaitAsync();

        try
        {
            _connection = await _factory.CreateConnectionAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
