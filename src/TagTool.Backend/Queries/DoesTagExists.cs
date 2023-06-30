using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Queries;

public class DoesTagExistsQuery : IQuery<bool>
{
    public required TagBase TagBase { get; init; }
}

[UsedImplicitly]
public class DoesTagExists : IQueryHandler<DoesTagExistsQuery, bool>
{
    private readonly TagToolDbContext _dbContext;

    public DoesTagExists(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DoesTagExistsQuery request, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(tagBase => tagBase.FormattedName == request.TagBase.FormattedName, cancellationToken);

        return tag is not null;
    }
}
