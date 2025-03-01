using Coravel.Invocable;

namespace TagTool.BackendNew.Contracts;

// jobDescriptor ----factory (validates against jobDefinition)----> job

// used to create a new job
public class InvocableDescriptor
{
    // public string Name { get; set; }

    // Type should be assignable to IJob
    public required Type InvocableType { get; set; } // better use name (?) to avoid Type in grpc massage

    public required ITrigger Trigger { get; init; }

    public string Args { get; init; } = "";
}

public interface IInvocableConstrainsBase
{
    ITrigger[] AllowedTriggers { get; }

    Type AllowedArgsType { get; }
}

// this is registered at startup
public interface IInvocableConstrains<TInvocable> : IInvocableConstrainsBase where TInvocable : IInvocable;
