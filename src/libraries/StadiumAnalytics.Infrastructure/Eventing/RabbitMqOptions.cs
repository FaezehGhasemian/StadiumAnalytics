namespace StadiumAnalytics.Infrastructure.Eventing;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "stadium.events";
    public string QueueName { get; set; } = "stadium.sensor-events";
    public string RoutingKey { get; set; } = "sensor.event";
    public string DeadLetterExchange { get; set; } = "stadium.events.dlx";
    public string DeadLetterQueue { get; set; } = "stadium.sensor-events.dlq";
    public ushort PrefetchCount { get; set; } = 50;
}
