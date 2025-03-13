using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Contracts.Internal;

public interface ICronTriggeredInvocablesStorage
{
    Task<bool> Exists(CronTriggeredInvocableInfo info);
    Task Add(CronTriggeredInvocableInfo info, CancellationToken cancellationToken);
}
