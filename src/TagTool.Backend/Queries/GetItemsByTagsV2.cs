using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsV2Query : IQuery<IEnumerable<TaggedItemBase>>
{
    public required IEnumerable<TagQuerySegment> QuerySegments { get; init; }
}

[UsedImplicitly]
public class GetItemsByTagsV2 : IQueryHandler<GetItemsByTagsV2Query, IEnumerable<TaggedItemBase>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItemsByTagsV2(TagToolDbContext dbContext) => _dbContext = dbContext;

    public Task<IEnumerable<TaggedItemBase>> Handle(GetItemsByTagsV2Query request, CancellationToken cancellationToken)
    {
        var splittedTags = SplitTagsBySegmentState(request.QuerySegments);

        var taggedItems = _dbContext.TaggedItemsBase.Include(item => item.Tags).AsQueryable();

        if (splittedTags.TryGetValue(QuerySegmentState.MustBePresent, out var mustByPresentTags))
        {
            taggedItems = taggedItems.Where(item => item.Tags.Any(tag => mustByPresentTags.Contains(tag)));
        }
        else if (splittedTags.TryGetValue(QuerySegmentState.Include, out var included))
        {
            taggedItems = taggedItems.Where(item => item.Tags.Any(tag => included.Contains(tag)));
        }

        if (splittedTags.TryGetValue(QuerySegmentState.Exclude, out var excluded))
        {
            taggedItems = taggedItems.Where(item => !item.Tags.Any(tag => excluded.Contains(tag)));
        }

        return Task.FromResult<IEnumerable<TaggedItemBase>>(taggedItems);
    }

    private static Dictionary<QuerySegmentState, IEnumerable<TagBase>> SplitTagsBySegmentState(IEnumerable<TagQuerySegment> request) =>
        request
            .GroupBy(segment => segment.State)
            .ToDictionary(segments => segments.Key, segments => segments.Select(segment => segment.Tag));
}
