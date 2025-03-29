namespace TagTool.BackendNew.Contracts.Invocables;

public class InvocableDescriptor
{
    public required string InvocableId { get; set; }

    public required ITrigger Trigger { get; init; }

    public string Args { get; init; } = "";
}
