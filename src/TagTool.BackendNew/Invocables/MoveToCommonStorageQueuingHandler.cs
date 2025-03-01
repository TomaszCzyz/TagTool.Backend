using Coravel.Queuing.Interfaces;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Invocables;

public class MoveToCommonStorageQueuingHandler : QueuingHandler<MoveToCommonStorage, MoveToCommonStoragePayload>
{
    private readonly IQueue _queue;

    public MoveToCommonStorageQueuingHandler(IQueue queue)
    {
        _queue = queue;
    }

    protected override void Queue(MoveToCommonStoragePayload payload)
    {
        _queue.QueueInvocableWithPayload<MoveToCommonStorage, MoveToCommonStoragePayload>(payload);
    }
}
