using Coravel.Invocable;

namespace TagTool.BackendNew.Contracts;

public interface IQueuingHandlerBase
{
    void Queue(object payload);
}

// TODO: can I simplify class definition?
public abstract class QueuingHandler<TInvocable, TPayload> : IQueuingHandlerBase
    where TInvocable : IInvocableWithPayload<TPayload>
{
    public void Queue(object payload) => Queue((TPayload)payload);

    protected abstract void Queue(TPayload payload);
}
