using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class CreateTagRequest : ICommand<OneOf<TagBase, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public IReversible GetReverse() => new Commands.DeleteTagRequest { Tag = Tag };
}

[UsedImplicitly]
public class CreateTag : ICommandHandler<CreateTagRequest, OneOf<TagBase, ErrorResponse>>
{
    private readonly ILogger<CreateTag> _logger;
    private readonly TagToolDbContext _dbContext;

    public CreateTag(ILogger<CreateTag> logger, TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<TagBase, ErrorResponse>> Handle(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var first = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == request.Tag.FormattedName, cancellationToken);

        if (first is not null)
        {
            return new ErrorResponse($"Tag {request.Tag} already exists.");
        }

        _logger.LogInformation("Creating new tag {@TagName}", request.Tag);

        await _dbContext.Tags.AddAsync(request.Tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return request.Tag;
    }
}
