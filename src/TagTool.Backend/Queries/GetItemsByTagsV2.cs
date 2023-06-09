using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsV2Query : IQuery<IEnumerable<TaggedItem>>
{
    public required IEnumerable<TagQuerySegment> QuerySegments { get; init; }
}

[UsedImplicitly]
public class GetItemsByTagsV2 : IQueryHandler<GetItemsByTagsV2Query, IEnumerable<TaggedItem>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItemsByTagsV2(TagToolDbContext dbContext) => _dbContext = dbContext;

    public Task<IEnumerable<TaggedItem>> Handle(GetItemsByTagsV2Query request, CancellationToken cancellationToken)
    {
        var groups = request.QuerySegments.GroupBy(segment => segment.Include).ToArray();

        var includeTags = groups.Where(b => b.Key).SelectMany(segments => segments).Select(segment => segment.Tag);
        var excludeTags = groups.Where(b => !b.Key).SelectMany(segments => segments).Select(segment => segment.Tag);

        var taggedItems = _dbContext.TaggedItems
            .Include(item => item.Tags)
            .Where(item => item.Tags.Any(tag => includeTags.Contains(tag)))
            .Where(item => !item.Tags.Any(tag => excludeTags.Contains(tag)))
            .ToArray();

        return Task.FromResult<IEnumerable<TaggedItem>>(taggedItems);
    }
}
