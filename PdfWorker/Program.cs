using Core;
using Core.Config;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PdfWorker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args); 
        builder.Configuration.SetBasePath(AppContext.BaseDirectory); 

        builder.AddConfig();

        builder.Services.AddHttpClient();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(CreateConnectionFactory);
        builder.Services.AddDbContextFactory<DatabaseContext>(CreateDbContext);
        builder.Services.AddHostedService<IncomingPdfsService>();

        var host = builder.Build();
        host.Run();
    }

    private static ConnectionFactory CreateConnectionFactory(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<CoreConfig>>().Value;

        return new()
        {
            HostName = options.RabbitAddress,
            Port = options.RabbitPort
        };
    }

    private static void CreateDbContext(IServiceProvider provider, DbContextOptionsBuilder builder)
    {
        var options = provider.GetRequiredService<IOptions<CoreConfig>>().Value;
        builder.UseNpgsql(options.DbConnectionString);
    }
}

file static class InitHelpers
{
    /// <summary>
    /// Регистрирует зависимости для конфигурации.
    /// </summary>
    public static void AddConfig(this HostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(Consts.ConfigFile, false);
        builder.Services.Configure<CoreConfig>(builder.Configuration.GetSection("Core"));
    }
}
