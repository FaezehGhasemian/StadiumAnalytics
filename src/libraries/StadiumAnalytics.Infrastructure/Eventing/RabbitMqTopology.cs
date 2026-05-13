using RabbitMQ.Client;

namespace StadiumAnalytics.Infrastructure.Eventing;

public static class RabbitMqTopology
{
    public static void Declare(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(
            exchange: options.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: options.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: options.DeadLetterQueue,
            exchange: options.DeadLetterExchange,
            routingKey: options.RoutingKey);

        channel.ExchangeDeclare(
            exchange: options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", options.DeadLetterExchange },
            { "x-dead-letter-routing-key", options.RoutingKey }
        };

        channel.QueueDeclare(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        channel.QueueBind(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey);
    }
}
