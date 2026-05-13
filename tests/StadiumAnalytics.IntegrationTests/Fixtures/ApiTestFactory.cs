using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.IntegrationTests.Fixtures;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public ApiTestFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Swap the DbContext to use the shared in-memory SQLite connection
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Remove any IHostedService registrations that might have leaked in
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var d in hostedServiceDescriptors)
                services.Remove(d);

            // Replace IEventBus with a no-op so tests don't talk to RabbitMQ
            var eventBusDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEventBus));
            if (eventBusDescriptor != null)
                services.Remove(eventBusDescriptor);

            services.AddSingleton<IEventBus, NullEventBus>();
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    private sealed class NullEventBus : IEventBus
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : class
            => Task.CompletedTask;
    }
}
