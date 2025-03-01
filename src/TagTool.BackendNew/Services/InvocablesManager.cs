using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Services;

public class InvocablesManager
{
    private readonly IEventTriggeredInvocablesStorage _eventTriggeredInvocablesStorage;

    public InvocablesManager(IEventTriggeredInvocablesStorage eventTriggeredInvocablesStorage)
    {
        _eventTriggeredInvocablesStorage = eventTriggeredInvocablesStorage;
    }

    /// <summary>
    /// Add invocable to storage and active it based on trigger type
    /// </summary>
    /// <param name="invocableDescriptor"></param>
    public async Task AddAndActivateJob(InvocableDescriptor invocableDescriptor)
    {
        switch (invocableDescriptor.Trigger)
        {
            case ItemTaggedTrigger:
                // the invocable is fetched from db when event occurs, so no need for extra "run" setup
                await _eventTriggeredInvocablesStorage.Add(invocableDescriptor);
                break;
        }
    }
}
