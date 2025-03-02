using Coravel.Invocable;

namespace TagTool.BackendNew.Contracts;

public interface IInvocableDescriptionBase
{
    string Id { get; }

    string GroupId { get; }

    string DisplayName { get; }

    string Description { get; }
}

// ReSharper disable once UnusedTypeParameter // used via Reflection in InvocableManger for registeration
public interface IInvocableDescription<TInvocable> : IInvocableDescriptionBase where TInvocable : IInvocable;
