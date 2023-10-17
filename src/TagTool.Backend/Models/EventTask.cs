namespace TagTool.Backend.Models;

public interface IJustTask
{
}

public class EventTask : IJustTask
{
    public required string TaskId { get; init; }

    public required string ActionId { get; init; }

    public required Dictionary<string, string> ActionAttributes { get; init; }

    public required string[] Events { get; init; }
}

public class CronTask : IJustTask
{
    public required string TaskId { get; init; }

    public required string ActionId { get; init; }

    public required Dictionary<string, string> ActionAttributes { get; init; }

    public required TagQuery TagQuery { get; init; }

    public required string Cron { get; init; }
}
