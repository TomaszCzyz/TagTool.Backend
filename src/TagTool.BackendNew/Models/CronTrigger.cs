using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.Models;

public class CronTrigger : ITrigger
{
    public required string CronExpression { get; init; }

    public required List<TagQueryParam> Query { get; init; }
}
