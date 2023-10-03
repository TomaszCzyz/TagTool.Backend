using System.Collections.ObjectModel;

namespace TagTool.Backend.Jobs;

public record JobInfo(string Id, string? Description, IDictionary<string, string>? AttributesDescriptions);

public interface IJobFactory
{
    IJob? Create(string jobId);

    IReadOnlyCollection<JobInfo> GetAvailableJob();
}

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReadOnlyCollection<JobInfo> _jobInfos;

    public JobFactory(IServiceProvider serviceProvider, ReadOnlyCollection<JobInfo> jobInfos)
    {
        _serviceProvider = serviceProvider;
        _jobInfos = jobInfos;
    }

    public IJob? Create(string jobId)
    {
        var serviceScope = _serviceProvider.CreateScope();
        var t = serviceScope.ServiceProvider.GetKeyedService<IJob>(jobId);
        return t;
    }

    public IReadOnlyCollection<JobInfo> GetAvailableJob()
    {
        return _jobInfos;
    }
}
