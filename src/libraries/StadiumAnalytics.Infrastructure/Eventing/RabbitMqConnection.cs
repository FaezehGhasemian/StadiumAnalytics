using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace StadiumAnalytics.Infrastructure.Eventing;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private readonly object _lock = new();
    private bool _disposed;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
        }

        return _connection;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}
