using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.Models;

public class CronTrigger : ITrigger
{
    public required string CronExpression { get; init; }

    public required List<TagQueryPart> Query { get; init; }
}
