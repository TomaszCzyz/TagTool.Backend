namespace TagTool.Backend.Models;

public enum TriggerType
{
    Manual = 0,
    Cron = 1,
    Event = 2,
}

public class Trigger
{
    public TriggerType Type { get; init; }

    public string? Arg { get; init; }
}
