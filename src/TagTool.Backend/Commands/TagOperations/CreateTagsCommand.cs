using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands.TagOperations;

public class CreateTagsCommand : IRequest<List<Result>>
{
    public required string[] TagNames { get; init; }
}

[UsedImplicitly]
public class CreateTagsCommandHandler : IRequestHandler<CreateTagsCommand, List<Result>>
{
    private readonly List<Result> _results = new();

    private readonly ILogger<CreateTagsCommandHandler> _logger;

    public CreateTagsCommandHandler(ILogger<CreateTagsCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<List<Result>> Handle(CreateTagsCommand request, CancellationToken cancellationToken)
    {
        await using var db = new TagContext();

        var existingTags = db.Tags
            .Where(tag => request.TagNames.Contains(tag.Name))
            .Select(tag => tag.Name)
            .ToArray();

        var newTags = request.TagNames
            .Except(existingTags)
            .Select(tagName => new Tag { Name = tagName })
            .ToList();

        _logger.LogDebug("Inserting new tags {@TagNames}", newTags);
        db.Tags.AddRange(newTags);

        await db.SaveChangesAsync(cancellationToken);

        foreach (var existingTag in existingTags)
        {
            _results.Add(new Result { IsSuccess = false, Messages = { $"Tag {existingTag} already exists" } });
        }

        foreach (var newTag in newTags.Select(tag => tag.Name))
        {
            _results.Add(new Result { IsSuccess = true, Messages = { $"Tag {newTag} successfully created" } });
        }

        return _results;
    }
}
