using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsRequest : IQuery<IEnumerable<TaggedItemBase>>
{
    public required string[] TagNames { get; init; }
}

[UsedImplicitly]
public class GetItemsByTags : IQueryHandler<GetItemsByTagsRequest, IEnumerable<TaggedItemBase>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItemsByTags(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async Task<IEnumerable<TaggedItemBase>> Handle(GetItemsByTagsRequest request, CancellationToken cancellationToken)
        => await _dbContext.TaggedItemsBase
            .Include(item => item.Tags)
            .Where(item => item.Tags.OfType<NormalTag>().Any(tag => request.TagNames.Contains(tag.Name)))
            .Select(item
                => new { Item = item, CommonTagsCount = item.Tags.OfType<NormalTag>().Count(tag => request.TagNames.Contains(tag.Name)) })
            .OrderByDescending(arg => arg.CommonTagsCount)
            .Select(arg => arg.Item)
            .ToArrayAsync(cancellationToken);
}
