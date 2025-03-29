using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<Success, NotFound, Error<string>>;

public class InvokeOperation : ICommand<Response>
{
    public required string ItemId { get; init; }
    public required string OperationName { get; init; }
    public required string OperationArgs { get; init; }
}

public class InvokeOperationCommandHandler : ICommandHandler<InvokeOperation, Response>
{
    private readonly ITagToolDbContext _dbContext;

    public InvokeOperationCommandHandler(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(InvokeOperation request, CancellationToken cancellationToken)
    {
        var taggableItem = await _dbContext.TaggableItems.FindAsync([request.ItemId], cancellationToken);

        if (taggableItem is null)
        {
            return new NotFound();
        }

        // _operationManager.GetOperation(request.OperationName)

        throw new NotImplementedException();
    }
}
