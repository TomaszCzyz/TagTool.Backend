using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Queries;

using Response = OneOf<TagBase, NotFound>;

public class GetTagByText : IQuery<Response>
{
    public required string Text { get; init; }
}

[UsedImplicitly]
public class GetTagQueryHandler : IQueryHandler<GetTagByText, Response>
{
    private readonly ITagToolDbContext _dbContext;

    public GetTagQueryHandler(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(GetTagByText request, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(tagBase => tagBase.Text == request.Text, cancellationToken);

        return tag is not null ? tag : new NotFound();
    }
}
