using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsQuery : IQuery<IEnumerable<TaggableItem>>
{
    public required TagQuerySegment[] QuerySegments { get; init; }
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

        var taggedItems = _dbContext.TaggedItems
            .Include(taggedItemBase => taggedItemBase.Tags)
            .AsQueryable();

        if (splittedTags.TryGetValue(QuerySegmentState.Include, out var included))
        {
            taggedItems = taggedItems
                .Where(item => item.Tags
                    .Select(tagBase => tagBase.FormattedName)
                    .Any(tag => included.Contains(tag)));
        }

        if (splittedTags.TryGetValue(QuerySegmentState.Exclude, out var excluded))
        {
            taggedItems = taggedItems
                .Where(item => !item.Tags
                    .Select(tagBase => tagBase.FormattedName)
                    .Any(tag => excluded.Contains(tag)));
        }

        // Inner predicate cannot be translated by EF Core, so as a temporary workaround I will filter the results on a client side. 
        if (splittedTags.TryGetValue(QuerySegmentState.MustBePresent, out var mustByPresentTags))
        {
            var taggedItemsFiltered = await taggedItems.ToListAsync(cancellationToken);

            return taggedItemsFiltered
                .Where(item => mustByPresentTags
                    .All(mustTag => item.Tags.Select(tagBase => tagBase.FormattedName).Contains(mustTag)));
        }

        return await taggedItems.ToArrayAsync(cancellationToken);
    }

    private Task<TaggableItem[]> GetMostPopularItems(CancellationToken cancellationToken)
        => _dbContext.TaggedItems
            .Include(taggedItemBase => taggedItemBase.Tags)
            .OrderByDescending(item => item.Popularity)
            .Take(30)
            .ToArrayAsync(cancellationToken);

    private static Dictionary<QuerySegmentState, IEnumerable<string>> SplitTagsBySegmentState(IEnumerable<TagQuerySegment> request)
        => request
            .GroupBy(segment => segment.State)
            .ToDictionary(segments => segments.Key, segments => segments.Select(segment => segment.Tag.FormattedName));
}
