namespace Broker.Messages;

/// <summary>
/// Информация о загруженном файле.
/// </summary>
public readonly struct UploadedFileMessage : IMessage
{
    /// <summary>
    /// Название файла.
    /// </summary>
    public readonly required string FileName { get; init; }

    /// <summary>
    /// Ссылка для скачивания.
    /// </summary>
    public readonly required Uri DownloadUrl { get; init; }
}
