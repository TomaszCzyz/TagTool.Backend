using Coravel.Queuing.Interfaces;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Invocables;

public class MoveToCommonStorageQueuingHandler : IQueuingHandler<MoveToCommonStorage, MoveToCommonStoragePayload>
{
    private readonly IQueue _queue;

    public MoveToCommonStorageQueuingHandler(IQueue queue)
    {
        _queue = queue;
    }

    public void Queue(MoveToCommonStoragePayload payload)
    {
        _queue.QueueInvocableWithPayload<MoveToCommonStorage, MoveToCommonStoragePayload>(payload);
    }
}
