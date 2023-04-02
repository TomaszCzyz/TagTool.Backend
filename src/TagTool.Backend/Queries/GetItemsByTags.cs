using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemsByTagsRequest : IRequest<IEnumerable<TaggedItem>>
{
    public string[] TagNames { get; init; }
}

[UsedImplicitly]
public class GetItemsByTags : IRequestHandler<GetItemsByTagsRequest, IEnumerable<TaggedItem>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItemsByTags(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async Task<IEnumerable<TaggedItem>> Handle(GetItemsByTagsRequest request, CancellationToken cancellationToken)
        => await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .Where(item => item.Tags.Any(tag => request.TagNames.Contains(tag.Name)))
            .Select(item => new { Item = item, CommonTagsCount = item.Tags.Count(tag => request.TagNames.Contains(tag.Name)) })
            .OrderByDescending(arg => arg.CommonTagsCount)
            .Select(arg => arg.Item)
            .ToArrayAsync(cancellationToken);
}
