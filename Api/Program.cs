using Broker;
using Core;
using Core.Config;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        builder.AddConfig();

        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddOpenApi();
        builder.Services.AddDbContextFactory<DatabaseContext>(CreateDbContext);

        builder.Services.AddSingleton<ConnectionManager>();
        builder.Services.AddSingleton<Producer>();

        var app = builder.Build();

        app.MapControllers();

        //app.CreateOrMigrateDb();

        app.MapOpenApi();
        app.Run();
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
    public static void AddConfig(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(Consts.ConfigFile, false);
        builder.Services.Configure<CoreConfig>(builder.Configuration.GetSection("Core"));
    }

    /// <summary>
    /// Создает или мигрирует БД.
    /// </summary>
    public static void CreateOrMigrateDb(this WebApplication app)
    {
        var factory = app.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        using var dbContext = factory.CreateDbContext();
        dbContext.Database.Migrate();
    }
}
