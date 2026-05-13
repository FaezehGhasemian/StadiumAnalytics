using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Domain.Entities;
using StadiumAnalytics.Infrastructure.Eventing;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.Consumer;

public sealed class SensorEventConsumer : BackgroundService
{
    private readonly RabbitMqConnection _rabbitConnection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<SensorEventConsumer> _logger;
    private IModel? _channel;

    public SensorEventConsumer(
        RabbitMqConnection rabbitConnection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<SensorEventConsumer> logger)
    {
        _rabbitConnection = rabbitConnection;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = _rabbitConnection.GetConnection();
        _channel = connection.CreateModel();

        RabbitMqTopology.Declare(_channel, _options);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        stoppingToken.Register(() =>
        {
            _channel?.Close();
        });

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        ulong deliveryTag = ea.DeliveryTag;
        try
        {
            var message = JsonSerializer.Deserialize<SensorEventMessage>(ea.Body.Span);
            if (message is null)
                throw new ArgumentException("Failed to deserialize message.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var timestampUtc = DateTime.SpecifyKind(message.TimestampUtc, DateTimeKind.Utc);
            var sensorEvent = new SensorEvent(
                message.Gate,
                timestampUtc,
                message.NumberOfPeople,
                message.Type);

            db.SensorEvents.Add(sensorEvent);
            await db.SaveChangesAsync();

            _channel!.BasicAck(deliveryTag, multiple: false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Poison message received, sending to DLQ.");
            _channel?.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transient error processing message, requeueing.");
            _channel?.BasicNack(deliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
