using System.Diagnostics;
using Coravel.Queuing.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TagTool.BackendNew.Broadcasting;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables;
using TagTool.BackendNew.Notifications;
using TagTool.BackendNew.Services.Grpc.Dtos;

namespace TagTool.BackendNew.Services.Grpc;

public class JobService : InvocablesService.InvocablesServiceBase
{
    private readonly InvocablesManager _invocablesManager;
    private readonly IQueue _queue;

    public JobService(InvocablesManager invocablesManager, IQueue queue)
    {
        _invocablesManager = invocablesManager;
        _queue = queue;
    }

    public override async Task<CreateInvocableReply> CreateInvocable(CreateInvocableRequest request, ServerCallContext context)
    {
        var invocableType = request.Type switch
        {
            "MoveToCommonStorage" => typeof(MoveToCommonStorage),
            "CronMoveToCommonStorage" => typeof(CronMoveToCommonStorage),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, null)
        };

        ITrigger trigger = request.TriggerCase switch
        {
            CreateInvocableRequest.TriggerOneofCase.None => throw new ArgumentException("Trigger is required"),
            CreateInvocableRequest.TriggerOneofCase.EventTrigger => ItemTaggedTrigger.Instance,
            CreateInvocableRequest.TriggerOneofCase.CronTrigger => new CronTrigger { CronExpression = request.CronTrigger.CronExpression },
            _ => throw new UnreachableException()
        };

        var invocableDescriptor = new InvocableDescriptor
        {
            InvocableType = invocableType,
            Trigger = trigger,
            Args = request.Args
        };


        await _invocablesManager.AddAndActivateJob(invocableDescriptor);

        return new CreateInvocableReply();
    }

    public override Task<Empty> BroadcastEvent(Empty request, ServerCallContext context)
    {
        var changeTypes = new Dictionary<Guid, ChangeType> { { Guid.NewGuid(), ChangeType.Added }, { Guid.NewGuid(), ChangeType.Removed } };
        _queue.QueueBroadcast(new ItemTagsChangedEvent(Guid.NewGuid(), changeTypes));

        return Task.FromResult(new Empty());
    }
}
