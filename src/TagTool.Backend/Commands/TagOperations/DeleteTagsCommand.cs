using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.Models;
using TagTool.Backend.Repositories;

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
    private readonly IConnectionsFactory _connectionsFactory;

    public DeleteTagsCommandHandler(ILogger<DeleteTagsCommandHandler> logger, IConnectionsFactory connectionsFactory)
    {
        _logger = logger;
        _connectionsFactory = connectionsFactory;
    }

    public Task<List<Result>> Handle(DeleteTagsCommand request, CancellationToken cancellationToken)
    {
        using var db = _connectionsFactory.Create();
        var tagsCollection = db.GetCollection<Tag>("Tags");

        var existingTags = tagsCollection
            .Query()
            .Where(tag => request.TagNames.Contains(tag.Name))
            .ToArray();

        foreach (var existingTag in existingTags)
        {
            var isDeleted = tagsCollection.Delete(existingTag.Id);
            if (!isDeleted) continue;

            _logger.LogInformation("Removed tag {@TagName} from database", existingTag.Name);
        }

        foreach (var existingTag in existingTags)
        {
            _results.Add(new Result { IsSuccess = true, Messages = { $"Tag {existingTag.Name} has been removed" } });
        }

        foreach (var nonExistingTag in request.TagNames.Except(existingTags.Select(tag => tag.Name)))
        {
            _results.Add(new Result { IsSuccess = false, Messages = { $"No tag {nonExistingTag} in a database" } });
        }

        return Task.FromResult(_results);
    }
}
