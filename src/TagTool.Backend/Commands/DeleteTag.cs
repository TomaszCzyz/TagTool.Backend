using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class DeleteTagRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string TagName { get; init; }

    public bool DeleteUsedToo { get; init; }
}

[UsedImplicitly]
public class DeleteTag : ICommandHandler<DeleteTagRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<DeleteTag> _logger;
    private readonly TagToolDbContext _dbContext;

    public DeleteTag(ILogger<DeleteTag> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(DeleteTagRequest request, CancellationToken cancellationToken)
    {
        var newTagName = request.TagName;
        var existingTag = await _dbContext.Tags
            .Include(tag => tag.TaggedItems)
            .FirstOrDefaultAsync(tag => tag.Name == newTagName, cancellationToken);

        if (existingTag is null)
        {
            return new ErrorResponse($"Tag {request.TagName} does not exists.");
        }

        if (!request.DeleteUsedToo && existingTag.TaggedItems.Count != 0)
        {
            var message = $"Tag {request.TagName} is in use and it was not deleted. " +
                          $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag.";

            return new ErrorResponse(message);
        }

        _logger.LogInformation("Removing tag {@TagName} and all its occurrences in TaggedItems table", existingTag);
        _dbContext.Tags.Remove(existingTag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return request.TagName;
    }
}
