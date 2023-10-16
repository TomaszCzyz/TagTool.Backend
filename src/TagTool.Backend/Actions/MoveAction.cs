using MediatR;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Actions;

public enum ActionResult
{
    Successful = 0,
    Failed = 1
}

public class MoveAction : IAction
{
    private readonly ILogger<MoveAction> _logger;
    private readonly IMediator _mediator;

    public string Id { get; } = "MoveFileJob";

    public string? Description { get; } = "The job that can move a file to a new location.";

    public IDictionary<string, string>? AttributesDescriptions { get; }
        = new Dictionary<string, string> { { "from", "TaggableFile.Path" }, { "to", "path/CommonStorage" } };

    public ItemTypeTag[] ItemTypes { get; } = { new() { Type = typeof(TaggableFile) } };

    /// <summary>
    ///     Ctor used to register the job.
    /// </summary>
    public MoveAction()
    {
        _logger = null!;
        _mediator = null!;
    }

    public MoveAction(ILogger<MoveAction> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<ActionResult> ExecuteOnSchedule(TagQuery tagQuery, Dictionary<string, string> data)
    {
        _logger.LogInformation("Executing job 'MoveFileJob' with args: {@TagQuery} and {@Attributes}", tagQuery, data);

        var command = new Commands.MoveFileRequest { OldFullPath = "", NewFullPath = "CommonStorage" };

        var reply = await _mediator.Send(command);

        return reply.Match(_ => ActionResult.Successful, _ => ActionResult.Failed);
    }

    public Task<ActionResult> ExecuteByEvent(IEnumerable<Guid> itemIds, Dictionary<string, string> data)
    {
        foreach (var taggableItem in itemIds)
        {
            // execute...
        }

        return Task.FromResult(ActionResult.Successful);
    }
}
