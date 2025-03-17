using Coravel.Scheduling.Schedule.Interfaces;
using NSubstitute;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Services;
using Xunit;

namespace TagTool.BackendNew.Tests.Unit.Services;

public class InvocablesManagerTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IScheduler _scheduler = Substitute.For<IScheduler>();
    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();

    private readonly InvocablesManager _sut;

    public InvocablesManagerTests()
    {
        _sut = new InvocablesManager(_serviceProvider, _scheduler, _dbContext, []);
    }

    [Fact]
    public void Ctor()
    {
    }
}
