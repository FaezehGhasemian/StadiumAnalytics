using Microsoft.EntityFrameworkCore;
using Serilog;
using StadiumAnalytics.Consumer;
using StadiumAnalytics.Infrastructure;
using StadiumAnalytics.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((sp, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console());

    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddMessaging(builder.Configuration);
    builder.Services.AddSensorEventConsumer();

    var host = builder.Build();

    if (!host.Services.GetRequiredService<IHostEnvironment>().IsEnvironment("Testing"))
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DatabaseMigrator.MigrateWithLockAsync(db, logger);
    }

    host.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Consumer terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
