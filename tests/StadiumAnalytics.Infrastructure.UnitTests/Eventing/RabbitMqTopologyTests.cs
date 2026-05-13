using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using StadiumAnalytics.Infrastructure.Eventing;

namespace StadiumAnalytics.Infrastructure.UnitTests.Eventing;

public class RabbitMqTopologyTests
{
    private readonly RabbitMqOptions _options = new();
    private readonly Mock<IModel> _channel = new(MockBehavior.Loose);

    [Fact]
    public void Declare_DeclaresDeadLetterExchangeAsDurableDirect()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.ExchangeDeclare(
            _options.DeadLetterExchange,
            ExchangeType.Direct,
            true,
            false,
            null), Times.Once);
    }

    [Fact]
    public void Declare_DeclaresMainExchangeAsDurableDirect()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.ExchangeDeclare(
            _options.ExchangeName,
            ExchangeType.Direct,
            true,
            false,
            null), Times.Once);
    }

    [Fact]
    public void Declare_DeclaresDeadLetterQueue_DurableNonExclusive()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.QueueDeclare(
            _options.DeadLetterQueue,
            true,
            false,
            false,
            null), Times.Once);
    }

    [Fact]
    public void Declare_DeclaresMainQueue_WithDeadLetterArguments()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.QueueDeclare(
            _options.QueueName,
            true,
            false,
            false,
            It.Is<IDictionary<string, object>>(args =>
                (string)args["x-dead-letter-exchange"] == _options.DeadLetterExchange &&
                (string)args["x-dead-letter-routing-key"] == _options.RoutingKey)),
            Times.Once);
    }

    [Fact]
    public void Declare_BindsDeadLetterQueueToDeadLetterExchange()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.QueueBind(
            _options.DeadLetterQueue,
            _options.DeadLetterExchange,
            _options.RoutingKey,
            null), Times.Once);
    }

    [Fact]
    public void Declare_BindsMainQueueToMainExchange()
    {
        RabbitMqTopology.Declare(_channel.Object, _options);

        _channel.Verify(c => c.QueueBind(
            _options.QueueName,
            _options.ExchangeName,
            _options.RoutingKey,
            null), Times.Once);
    }

    [Fact]
    public void Declare_UsesProvidedOptionNames()
    {
        var custom = new RabbitMqOptions
        {
            ExchangeName = "ex.custom",
            QueueName = "q.custom",
            RoutingKey = "rk.custom",
            DeadLetterExchange = "dlx.custom",
            DeadLetterQueue = "dlq.custom"
        };

        RabbitMqTopology.Declare(_channel.Object, custom);

        _channel.Verify(c => c.ExchangeDeclare("ex.custom", ExchangeType.Direct, true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeDeclare("dlx.custom", ExchangeType.Direct, true, false, null), Times.Once);
        _channel.Verify(c => c.QueueBind("q.custom", "ex.custom", "rk.custom", null), Times.Once);
        _channel.Verify(c => c.QueueBind("dlq.custom", "dlx.custom", "rk.custom", null), Times.Once);
    }
}
