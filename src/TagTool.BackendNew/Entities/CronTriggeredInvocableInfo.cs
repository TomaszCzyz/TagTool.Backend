using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Entities;

public class CronTriggeredInvocableInfo : InvocableInfoBase
{
    public required string CronExpression { get; set; }

    public required List<TagQueryPart> TagQuery { get; set; }
}
