using Coravel.Invocable;

namespace TagTool.BackendNew.Contracts.Invocables;

public interface IQueuingHandlerBase
{
    void Queue(object payload);
}

// TODO: can I simplify definition?
public interface IQueuingHandler<TInvocable, in TPayload> : IQueuingHandlerBase
    where TInvocable : IInvocableWithPayload<TPayload>
{
    void IQueuingHandlerBase.Queue(object payload) => Queue((TPayload)payload);

    void Queue(TPayload payload);
}
