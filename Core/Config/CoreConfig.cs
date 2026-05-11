namespace Core.Config;

public sealed class CoreConfig
{
    public string RabbitAddress { get; set; }
    public int RabbitPort { get; set; }
    public string DbConnectionString { get; set; }
}
