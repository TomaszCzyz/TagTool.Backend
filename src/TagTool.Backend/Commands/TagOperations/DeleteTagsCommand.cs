using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands.TagOperations;

public class DeleteTagsCommand : IRequest<List<Result>>
{
    public required string[] TagNames { get; init; }
}

[UsedImplicitly]
public class DeleteTagsCommandHandler : IRequestHandler<DeleteTagsCommand, List<Result>>
{
    private readonly List<Result> _results = new();

    private readonly ILogger<DeleteTagsCommandHandler> _logger;

    public DeleteTagsCommandHandler(ILogger<DeleteTagsCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<List<Result>> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
    {
        await using var db = new TagContext();

        var existingTags = db.Tags
            .Where(tag => request.TagNames.Contains(tag.Name))
            .ToArray();

        _logger.LogInformation("Removing tags {@TagNames} from database", existingTags.Select(tag => tag.Name));
        db.Tags.RemoveRange(existingTags);

        await db.SaveChangesAsync(cancellationToken);

        foreach (var existingTag in existingTags)
        {
            _results.Add(new Result { IsSuccess = true, Messages = { $"Tag {existingTag.Name} has been removed" } });
        }

        foreach (var nonExistingTag in request.TagNames.Except(existingTags.Select(tag => tag.Name)))
        {
            _results.Add(new Result { IsSuccess = false, Messages = { $"No tag {nonExistingTag} in a database" } });
        }

        return _results;
    }
}
