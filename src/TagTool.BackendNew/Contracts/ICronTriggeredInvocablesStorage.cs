using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Contracts;

public class CronTriggeredInvocableInfo
{
    public required Type InvocableType { get; init; }
    public required Type PayloadType { get; init; }
    public required string CronExpression { get; init; }
    public required PayloadWithQuery Args { get; init; }
}

public interface ICronTriggeredInvocablesStorage
{
    Task<bool> Exists(CronTriggeredInvocableInfo info);
    Task Add(CronTriggeredInvocableInfo info);
}
