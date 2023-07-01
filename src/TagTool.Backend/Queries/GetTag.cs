using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Queries;

public class GetTagQuery : IQuery<TagBase?>
{
    public required TagBase TagBase { get; init; }
}

[UsedImplicitly]
public class GetTag : IQueryHandler<GetTagQuery, TagBase?>
{
    private readonly TagToolDbContext _dbContext;

    public GetTag(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TagBase?> Handle(GetTagQuery request, CancellationToken cancellationToken)
        => await _dbContext.Tags.FirstOrDefaultAsync(tagBase => tagBase.FormattedName == request.TagBase.FormattedName, cancellationToken);
}
