using MediatR;

namespace TagTool.BackendNew.Services.Grpc;

public class JobService : BackendNew.JobService.JobServiceBase
{
    private readonly ILogger<JobService> _logger;
    private readonly IMediator _mediator;

    public JobService(ILogger<JobService> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
}
