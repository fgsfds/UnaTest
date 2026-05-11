using Core;
using Database;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace PdfWorker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

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
        return new()
        {
            HostName = Consts.RabbitAddress,
            Port = Consts.RabbitPort
        };
    }

    private static void CreateDbContext(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql(Consts.DbConnectionString);
    }
}
