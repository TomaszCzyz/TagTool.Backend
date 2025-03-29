using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<TagBase, Error<string>>;

public class DeleteTagRequest : ICommand<Response>
{
    public required int Id { get; init; }

    public bool DeleteUsedToo { get; init; }
}

[UsedImplicitly]
public class DeleteTag : ICommandHandler<DeleteTagRequest, Response>
{
    private readonly ILogger<DeleteTag> _logger;
    private readonly ITagToolDbContext _dbContext;

    public DeleteTag(ILogger<DeleteTag> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(DeleteTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags
            .Include(tag => tag.TaggedItems) // TODO: only count is needed
            .FirstOrDefaultAsync(tag => tag.Id == request.Id, cancellationToken);

        if (tag is null)
        {
            return new Error<string>("Tag not found");
        }

        if (!request.DeleteUsedToo && tag.TaggedItems.Count != 0)
        {
            var message = $"Tag {tag.Text} is in use and it was not deleted. " +
                          $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag.";

            return new Error<string>(message);
        }

        _logger.LogInformation("Removing tag {@TagName} and all its occurrences in TaggedItems table", tag.Text);
        _dbContext.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return tag;
    }
}
