using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Queries;

using Response = OneOf<TaggableItem, NotFound>;

public class GetItemById : IQuery<Response>
{
    public required Guid Id { get; init; }
}

[UsedImplicitly]
public class GetItemByIdQueryHandler : IQueryHandler<GetItemById, Response>
{
    private readonly ITagToolDbContext _dbContext;

    public GetItemByIdQueryHandler(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(GetItemById request, CancellationToken cancellationToken)
    {
        var item = await _dbContext.TaggableItems
            .AsNoTracking()
            .Include(file => file.Tags)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        return item is null ? new NotFound() : item;
    }
}
