namespace Core.Messages;

/// <summary>
/// Информация о загруженном файле.
/// </summary>
public class UploadedFileMessage
{
    /// <summary>
    /// Название файла.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Ссылка для скачивания.
    /// </summary>
    public required Uri DownloadUrl { get; init; }
}
