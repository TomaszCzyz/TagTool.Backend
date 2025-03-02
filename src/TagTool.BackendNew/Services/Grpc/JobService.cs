using Coravel.Queuing.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TagTool.BackendNew.Broadcasting;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables;
using TagTool.BackendNew.Notifications;

namespace TagTool.BackendNew.Services.Grpc;

public class JobService : BackendNew.JobService.JobServiceBase
{
    private readonly InvocablesManager _invocablesManager;
    private readonly IQueue _queue;

    public JobService(InvocablesManager invocablesManager, IQueue queue)
    {
        _invocablesManager = invocablesManager;
        _queue = queue;
    }

    public override async Task<CreateJobReply> CreateJob(CreateJobRequest request, ServerCallContext context)
    {
        var invocableDescriptor = new InvocableDescriptor
        {
            InvocableType = request.Type switch
            {
                "MoveToCommonStorage" => typeof(MoveToCommonStorage),
                _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, null)
            },
            Trigger = ItemTaggedTrigger.Instance,
            Args = request.Args
        };


        await _invocablesManager.AddAndActivateJob(invocableDescriptor);

        return new CreateJobReply();
    }

    public override Task<Empty> BroadcastEvent(Empty request, ServerCallContext context)
    {
        var changeTypes = new Dictionary<Guid, ChangeType> { { Guid.NewGuid(), ChangeType.Added }, { Guid.NewGuid(), ChangeType.Removed } };
        _queue.QueueBroadcast(new ItemTagsChangedEvent(Guid.NewGuid(), changeTypes));

        return Task.FromResult(new Empty());
    }
}
