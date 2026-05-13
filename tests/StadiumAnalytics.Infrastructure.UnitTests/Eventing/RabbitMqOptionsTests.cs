using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StadiumAnalytics.Infrastructure.Eventing;

namespace StadiumAnalytics.Infrastructure.UnitTests.Eventing;

public class RabbitMqOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var o = new RabbitMqOptions();

        o.HostName.Should().Be("localhost");
        o.Port.Should().Be(5672);
        o.UserName.Should().Be("guest");
        o.Password.Should().Be("guest");
        o.ExchangeName.Should().Be("stadium.events");
        o.QueueName.Should().Be("stadium.sensor-events");
        o.RoutingKey.Should().Be("sensor.event");
        o.DeadLetterExchange.Should().Be("stadium.events.dlx");
        o.DeadLetterQueue.Should().Be("stadium.sensor-events.dlq");
        o.PrefetchCount.Should().Be((ushort)50);
    }

    [Fact]
    public void BindFromConfiguration_AppliesAllValues()
    {
        var dict = new Dictionary<string, string?>
        {
            ["RabbitMq:HostName"]           = "rabbit.host",
            ["RabbitMq:Port"]               = "5673",
            ["RabbitMq:UserName"]           = "user",
            ["RabbitMq:Password"]           = "pwd",
            ["RabbitMq:ExchangeName"]       = "ex",
            ["RabbitMq:QueueName"]          = "q",
            ["RabbitMq:RoutingKey"]         = "rk",
            ["RabbitMq:DeadLetterExchange"] = "dlx",
            ["RabbitMq:DeadLetterQueue"]    = "dlq",
            ["RabbitMq:PrefetchCount"]      = "10"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var o = new RabbitMqOptions();
        config.GetSection(RabbitMqOptions.SectionName).Bind(o);

        o.HostName.Should().Be("rabbit.host");
        o.Port.Should().Be(5673);
        o.UserName.Should().Be("user");
        o.Password.Should().Be("pwd");
        o.ExchangeName.Should().Be("ex");
        o.QueueName.Should().Be("q");
        o.RoutingKey.Should().Be("rk");
        o.DeadLetterExchange.Should().Be("dlx");
        o.DeadLetterQueue.Should().Be("dlq");
        o.PrefetchCount.Should().Be((ushort)10);
    }

    [Fact]
    public void SectionName_IsRabbitMq()
    {
        RabbitMqOptions.SectionName.Should().Be("RabbitMq");
    }
}
