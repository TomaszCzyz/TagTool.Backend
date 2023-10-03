using MediatR;
using TagTool.Backend.Models;

namespace TagTool.Backend.Jobs;

public enum JobResult
{
    Successful = 0,
    Failed = 1
}

/// <summary>
///     Base class for jobs performed on <see cref="TaggableItem"/>s
/// </summary>
/// <remarks>
///     Every job type should have unique <see cref="Id"/>.
///     It could be enforced by making JobId field static. However, <see cref="IJob"/>
///     interface could not by used as type parameter then, what would make registration of a jobs
///     much more complex.
///     Do not forget to assign unique value to <see cref="Id"/>
/// </remarks>
public interface IJob
{
    string Id { get; }

    string? Description { get; }

    IDictionary<string, string>? AttributesDescriptions { get; }

    Task<JobResult> Execute(TagQuery tagQuery, Dictionary<string, string> data);

    // when execution is triggered by event 
    // Task<JobResult> Execute(TaggableItem item, Dictionary<string, string> data);
}

public class MoveJob : IJob
{
    private readonly ILogger<MoveJob> _logger;
    private readonly IMediator _mediator;

    public string Id { get; } = "MoveFileJob";

    public string? Description { get; } = "The job that can move a file to a new location.";

    public IDictionary<string, string>? AttributesDescriptions { get; }
        = new Dictionary<string, string> { { "from", "TaggableFile.Path" }, { "to", "path/CommonStorage" } };

    /// <summary>
    ///     Ctor used to register the job.
    /// </summary>
    public MoveJob()
    {
        _logger = null!;
        _mediator = null!;
    }

    public MoveJob(ILogger<MoveJob> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<JobResult> Execute(TagQuery tagQuery, Dictionary<string, string> data)
    {
        _logger.LogInformation("Executing job 'MoveFileJob' with args: {@TagQuery} and {@Attributes}", tagQuery, data);

        var command = new Commands.MoveFileRequest { OldFullPath = "", NewFullPath = "CommonStorage" };

        var reply = await _mediator.Send(command);

        return reply.Match(_ => JobResult.Successful, _ => JobResult.Failed);
    }
}
