using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StadiumAnalytics.Application.Abstractions.Eventing;

namespace StadiumAnalytics.Infrastructure.Eventing;

public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqEventBus(RabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    private IModel GetChannel()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        _channel = _connection.GetConnection().CreateModel();
        RabbitMqTopology.Declare(_channel, _options);
        _channel.ConfirmSelect();
        return _channel;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var channel = GetChannel();
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);

            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2; // persistent
            props.MessageId = Guid.NewGuid().ToString();
            props.Type = typeof(TEvent).Name;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                basicProperties: props,
                body: body);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _channel?.Dispose();
        _semaphore.Dispose();
    }
}
