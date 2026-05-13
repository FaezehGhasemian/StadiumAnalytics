using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StadiumAnalytics.Simulator;

namespace StadiumAnalytics.Simulator.UnitTests;

public class SimulatorOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var o = new SimulatorOptions();

        o.Gates.Should().Equal("Gate A", "Gate B", "Gate C", "Gate D");
        o.IntervalMilliseconds.Should().Be(1000);
        o.MaxPeoplePerEvent.Should().Be(25);
    }

    [Fact]
    public void SectionName_IsSimulator()
    {
        SimulatorOptions.SectionName.Should().Be("Simulator");
    }

    [Fact]
    public void BindFromConfiguration_AppliesAllValues()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Simulator:Gates:0"]            = "Gate X",
            ["Simulator:Gates:1"]            = "Gate Y",
            ["Simulator:IntervalMilliseconds"] = "250",
            ["Simulator:MaxPeoplePerEvent"]    = "100"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var o = new SimulatorOptions();
        o.Gates = Array.Empty<string>();
        config.GetSection(SimulatorOptions.SectionName).Bind(o);

        o.Gates.Should().Equal("Gate X", "Gate Y");
        o.IntervalMilliseconds.Should().Be(250);
        o.MaxPeoplePerEvent.Should().Be(100);
    }
}
