using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Queries;

public class GetItemsByTagsQuery : IQuery<IEnumerable<TaggableItem>>
{
    public required TagQueryParam[] QuerySegments { get; init; }
}

[UsedImplicitly]
public class GetItemsByTags : IQueryHandler<GetItemsByTagsQuery, IEnumerable<TaggableItem>>
{
    private readonly ITagToolDbContext _dbContext;

    public GetItemsByTags(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<TaggableItem>> Handle(GetItemsByTagsQuery request, CancellationToken cancellationToken)
    {
        if (request.QuerySegments.Length == 0)
        {
            return await GetMostPopularItems(cancellationToken);
        }

        var splittedTags = SplitTagsBySegmentState(request.QuerySegments);

        var taggedItems = _dbContext.TaggableItems
            .Include(taggedItemBase => taggedItemBase.Tags)
            .AsQueryable();

        if (splittedTags.TryGetValue(QueryPartState.Include, out var included))
        {
            taggedItems = taggedItems.Where(item => item.Tags.Any(tag => included.Contains(tag.Id)));
        }

        if (splittedTags.TryGetValue(QueryPartState.Exclude, out var excluded))
        {
            taggedItems = taggedItems.Where(item => !item.Tags.Any(tag => excluded.Contains(tag.Id)));
        }

        // Inner predicate cannot be translated by EF Core, so as a temporary workaround I will filter the results on a client side.
        if (splittedTags.TryGetValue(QueryPartState.MustBePresent, out var mustByPresentTags))
        {
            var taggedItemsFiltered = await taggedItems.ToListAsync(cancellationToken);

            return taggedItemsFiltered
                .Where(item => mustByPresentTags
                    .All(mustTag => item.Tags.Select(tagBase => tagBase.Id).Contains(mustTag)));
        }

        return await taggedItems.ToArrayAsync(cancellationToken);
    }

    private Task<TaggableItem[]> GetMostPopularItems(CancellationToken cancellationToken)
        => _dbContext.TaggableItems
            .Include(taggedItemBase => taggedItemBase.Tags)
            // .OrderByDescending(item => item.Popularity)
            .Take(30)
            .ToArrayAsync(cancellationToken);

    private static Dictionary<QueryPartState, IEnumerable<int>> SplitTagsBySegmentState(
        IEnumerable<TagQueryParam> request)
        => request
            .GroupBy(segment => segment.State)
            .ToDictionary(segments => segments.Key, segments => segments.Select(segment => segment.TagId));
}
