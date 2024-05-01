using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemByIdQuery : IQuery<TaggableItem?>
{
    public required Guid Id { get; init; }
}

[UsedImplicitly]
public class GetItemById : IQueryHandler<GetItemByIdQuery, TaggableItem?>
{
    private readonly ITagToolDbContext _dbContext;

    public GetItemById(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaggableItem?> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
        => await _dbContext.TaggedItems
            .Include(file => file.Tags)
            .FirstOrDefaultAsync(item1 => item1.Id == request.Id, cancellationToken);
}
