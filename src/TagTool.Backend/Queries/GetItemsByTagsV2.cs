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
        var (includeTags, excludeTags) = SplitTags(request.QuerySegments);

        var included = _dbContext.Tags.Where(tagBase => includeTags.Contains(tagBase.FormattedName)).ToArray();
        var excluded = _dbContext.Tags.Where(tagBase => excludeTags.Contains(tagBase.FormattedName)).ToArray();

        var taggedItems = _dbContext.TaggedItemsBase
            .Include(item => item.Tags)
            .Where(item => item.Tags.Any(tag => included.Contains(tag)))
            .Where(item => !item.Tags.Any(tag => excluded.Contains(tag)))
            .ToArray();

        return Task.FromResult<IEnumerable<TaggedItemBase>>(taggedItems);
    }

    private static (string?[] includeTags, string?[] excludeTags) SplitTags(IEnumerable<TagQuerySegment> request)
    {
        var groups = request.GroupBy(segment => segment.Include).ToArray();

        var includeTags = groups.Where(b => b.Key).SelectMany(segments => segments).Select(segment => segment.Tag.FormattedName).ToArray();
        var excludeTags = groups.Where(b => !b.Key).SelectMany(segments => segments).Select(segment => segment.Tag.FormattedName).ToArray();

        return (includeTags, excludeTags);
    }
}
