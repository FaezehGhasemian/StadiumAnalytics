using Serilog;
using StadiumAnalytics.Infrastructure;
using StadiumAnalytics.Simulator;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((sp, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console());

    builder.Services.AddMessaging(builder.Configuration);
    builder.Services.AddSimulator(builder.Configuration);

    var host = builder.Build();
    host.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Simulator terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
