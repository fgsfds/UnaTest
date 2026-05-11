namespace Broker.Messages;

/// <summary>
/// Обертка над сообщением, содержащая дополнительную информацию.
/// </summary>
public readonly struct MessageWrapper<T>() where T : IMessage
{
    public readonly required ulong DeliveryTag { get; init; }

    public readonly required T Message { get; init; }
}
