namespace TagTool.BackendNew.Models;

public enum TriggerType
{
    Event,
    Cron,
    Background,
}

public record InvocableDefinition(
    string Id,
    string GroupId,
    string DisplayName,
    string Description,
    string PayloadSchema,
    TriggerType TriggerType,
    Type InvocableType);
