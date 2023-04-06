using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class CreateTagRequest : ICommand<OneOf<string, ErrorResponse>>, IReversible
{
    public required string TagName { get; init; }

    public IReversible GetReverse() => new DeleteTagRequest { TagName = TagName };
}

[UsedImplicitly]
public class CreateTag : ICommandHandler<CreateTagRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<CreateTag> _logger;
    private readonly TagToolDbContext _dbContext;

    public CreateTag(ILogger<CreateTag> logger, TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var newTagName = request.TagName;
        var first = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Name == newTagName, cancellationToken);

        if (first is not null)
        {
            return new ErrorResponse($"Tag {newTagName} already exists.");
        }

        _logger.LogInformation("Creating new tag {@TagName}", newTagName);

        await _dbContext.Tags.AddAsync(new Tag { Name = newTagName }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newTagName;
    }
}
