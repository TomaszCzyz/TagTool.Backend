using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Models;

public class CronTrigger : ITrigger
{
    public required string CronExpression { get; init; }

    public required List<TagQueryParam> Query { get; init; }
}
