namespace Core;

//TODO перенести в конфиг
public static class Consts
{
    public const string QueuePdfIncoming = "pdf.incoming";
    public const string RoutingKeyPdfUploaded = "pdf.uploaded";
    public const string ExchangeTopicAmq = "amq.topic";

    public const string RabbitAddress = "localhost";
    public const int RabbitPort = 5672;

    public const string DbConnectionString = "Host=localhost;Port=5432;Database=maindb;Username=postgres;Password=123;Include Error Detail=True";
        
    public const string FileStorageUrl = "htts://storage.mydomain.ru/";
}
