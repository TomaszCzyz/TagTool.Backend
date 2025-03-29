using Coravel.Invocable;
using TagTool.BackendNew.Contracts.Invocables.Common;

namespace TagTool.BackendNew.Contracts.Invocables;

public interface IEventTriggeredInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithChangedItems;

public interface ICronTriggeredInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithQuery;

public interface IBackgroundInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithQuery;
