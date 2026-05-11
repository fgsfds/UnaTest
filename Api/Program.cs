using Core;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<RabbitConnectionManager>();

        builder.Services.AddDbContextFactory<DatabaseContext>(CreateDbContext);

        var app = builder.Build();

        app.MapControllers();

        // создаем или мигрируем БД до запуска сервера
        var factory = app.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();

        app.MapOpenApi();
        app.Run();
    }

    private static void CreateDbContext(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql(Consts.DbConnectionString);
    }
}
