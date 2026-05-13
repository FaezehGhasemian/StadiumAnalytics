namespace StadiumAnalytics.Simulator;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string[] Gates { get; set; } = ["Gate A", "Gate B", "Gate C", "Gate D"];
    public int IntervalMilliseconds { get; set; } = 1000;
    public int MaxPeoplePerEvent { get; set; } = 25;
}
