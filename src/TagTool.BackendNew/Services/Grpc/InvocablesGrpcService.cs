using System.Diagnostics;
using Coravel.Queuing.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using TagTool.BackendNew.Contracts.Broadcasting;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Mappers;
using TagTool.BackendNew.Models;
using TagTool.BackendNew.Services.Grpc.Dtos;
using BackgroundTrigger = TagTool.BackendNew.Models.BackgroundTrigger;
using CronTrigger = TagTool.BackendNew.Models.CronTrigger;

namespace TagTool.BackendNew.Services.Grpc;

public class InvocablesGrpcService : InvocablesService.InvocablesServiceBase
{
    private readonly InvocablesManager _invocablesManager;
    private readonly IQueue _queue;

    public InvocablesGrpcService(InvocablesManager invocablesManager, IQueue queue)
    {
        _invocablesManager = invocablesManager;
        _queue = queue;
    }

    public override Task<GetInvocablesDescriptionsReply> GetInvocablesDescriptions(GetInvocablesDescriptionsRequest request,
        ServerCallContext context)
    {
        var replies = _invocablesManager
            .GetInvocableDefinitions()
            .Select(d => new GetInvocablesDescriptionsReply.Types.InvocableDefinition
            {
                Id = d.Id,
                GroupId = d.GroupId,
                DisplayName = d.DisplayName,
                Description = d.Description,
                PayloadSchema = d.PayloadSchema,
                TriggerType = d.TriggerType.ToString(),
            });

        return Task.FromResult(new GetInvocablesDescriptionsReply
        {
            InvocableDefinitions =
            {
                replies
            }
        });
    }

    public override async Task<CreateInvocableReply> CreateInvocable(CreateInvocableRequest request, ServerCallContext context)
    {
        ITrigger trigger = request.TriggerCase switch
        {
            CreateInvocableRequest.TriggerOneofCase.None => throw new ArgumentException("Trigger is required"),
            CreateInvocableRequest.TriggerOneofCase.EventTrigger => ItemTaggedTrigger.Instance,
            CreateInvocableRequest.TriggerOneofCase.CronTrigger => new CronTrigger
            {
                CronExpression = request.CronTrigger.CronExpression,
                Query = request.CronTrigger.QueryParams.MapFromDto()
            },
            CreateInvocableRequest.TriggerOneofCase.BackgroundTrigger => new BackgroundTrigger(),
            _ => throw new UnreachableException()
        };

        var invocableDescriptor = new InvocableDescriptor
        {
            InvocableId = request.InvocableId,
            Trigger = trigger,
            Args = request.Args
        };

        await _invocablesManager.AddAndActivateInvocable(invocableDescriptor, context.CancellationToken);

        return new CreateInvocableReply();
    }

    public override Task<Empty> BroadcastEvent(Empty request, ServerCallContext context)
    {
        var changeTypes = new Dictionary<Guid, ChangeType>
        {
            {
                Guid.NewGuid(), ChangeType.Added
            },
            {
                Guid.NewGuid(), ChangeType.Removed
            }
        };
        _queue.QueueBroadcast(new ItemTagsChangedEvent(Guid.NewGuid(), changeTypes));

        return Task.FromResult(new Empty());
    }
}
