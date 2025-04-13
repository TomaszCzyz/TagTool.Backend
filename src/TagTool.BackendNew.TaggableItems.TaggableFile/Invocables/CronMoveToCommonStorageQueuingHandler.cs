using Coravel.Queuing.Interfaces;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Invocables;

public class CronMoveToCommonStorageQueuingHandler : IQueuingHandler<CronMoveToCommonStorage, CronMoveToCommonStoragePayload>
{
    private readonly IQueue _queue;

    public CronMoveToCommonStorageQueuingHandler(IQueue queue)
    {
        _queue = queue;
    }

    public void Queue(CronMoveToCommonStoragePayload payload)
    {
        _queue.QueueInvocableWithPayload<CronMoveToCommonStorage, CronMoveToCommonStoragePayload>(payload);
    }
}
