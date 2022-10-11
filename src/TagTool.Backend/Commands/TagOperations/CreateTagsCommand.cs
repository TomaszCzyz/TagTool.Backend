using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.Models;
using TagTool.Backend.Repositories;

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
    private readonly IConnectionsFactory _connectionsFactory;

    public CreateTagsCommandHandler(ILogger<CreateTagsCommandHandler> logger, IConnectionsFactory connectionsFactory)
    {
        _logger = logger;
        _connectionsFactory = connectionsFactory;
    }

    public Task<List<Result>> Handle(CreateTagsCommand request, CancellationToken cancellationToken)
    {
        using var db = _connectionsFactory.Create();
        var tagsCollection = db.GetCollection<Tag>("Tags");

        var existingTags = tagsCollection
            .Query()
            .Where(tag => request.TagNames.Contains(tag.Name))
            .Select(tag => tag.Name)
            .ToArray();

        var newTags = request.TagNames
            .Except(existingTags)
            .Select(tagName => new Tag { Name = tagName })
            .ToList();

        _logger.LogDebug("Inserting new tags {@TagNames}", newTags);
        tagsCollection.Insert(newTags);

        foreach (var existingTag in existingTags)
        {
            _results.Add(new Result { IsSuccess = false, Messages = { $"Tag {existingTag} already exists" } });
        }

        foreach (var newTag in newTags.Select(tag => tag.Name))
        {
            _results.Add(new Result { IsSuccess = true, Messages = { $"Tag {newTag} successfully created" } });
        }

        return Task.FromResult(_results);
    }
}
