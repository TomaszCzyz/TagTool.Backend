using TagTool.Backend.Actions;

namespace TagTool.Backend.Queries;

public class GetAvailableActionsQuery : IQuery<IEnumerable<Actions.ActionInfo>>
{
}

public class GetAvailableActions : IQueryHandler<GetAvailableActionsQuery, IEnumerable<Actions.ActionInfo>>
{
    private readonly IActionFactory _actionFactory;

    public GetAvailableActions(IActionFactory actionFactory)
    {
        _actionFactory = actionFactory;
    }

    public Task<IEnumerable<Actions.ActionInfo>> Handle(GetAvailableActionsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_actionFactory.GetAvailableActions().AsEnumerable());
    }
}
