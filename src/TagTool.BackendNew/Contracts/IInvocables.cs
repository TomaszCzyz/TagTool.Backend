using Coravel.Invocable;
using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Contracts;

public interface IEventTriggeredInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithChangedItems;

public interface ICronTriggeredInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithQuery;

public interface IBackgroundInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithQuery;
