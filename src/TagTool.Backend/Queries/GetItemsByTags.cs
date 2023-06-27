using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsQuery : IQuery<IEnumerable<TaggableItem>>
{
    public required IEnumerable<TagQuerySegment> QuerySegments { get; init; }
}

[UsedImplicitly]
public class GetItemsByTags : IQueryHandler<GetItemsByTagsQuery, IEnumerable<TaggableItem>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItemsByTags(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IEnumerable<TaggableItem>> Handle(GetItemsByTagsQuery request, CancellationToken cancellationToken)
    {
        var splittedTags = SplitTagsBySegmentState(request.QuerySegments);

        var taggedItems = _dbContext.TaggedItems
            .Include(taggedItemBase => taggedItemBase.Tags)
            .AsQueryable();

        if (splittedTags.TryGetValue(QuerySegmentState.MustBePresent, out var mustByPresentTags))
        {
            taggedItems = taggedItems
                .Where(item => item.Tags
                    .Select(tagBase => tagBase.FormattedName)
                    .Any(tag => mustByPresentTags.Contains(tag)));
        }
        else if (splittedTags.TryGetValue(QuerySegmentState.Include, out var included))
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

        return Task.FromResult<IEnumerable<TaggableItem>>(taggedItems.ToArray());
    }

    private static Dictionary<QuerySegmentState, IEnumerable<string>> SplitTagsBySegmentState(IEnumerable<TagQuerySegment> request)
        => request
            .GroupBy(segment => segment.State)
            .ToDictionary(segments => segments.Key, segments => segments.Select(segment => segment.Tag.FormattedName));
}
