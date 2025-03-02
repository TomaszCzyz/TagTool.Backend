using Coravel.Scheduling.Schedule.Interfaces;
using NSubstitute;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Services;
using Xunit;

namespace TagTool.BackendNew.Tests.Unit.Services;

public class InvocablesManagerTests
{
    private readonly IEventTriggeredInvocablesStorage _eventTriggeredInvocablesStorage = Substitute.For<IEventTriggeredInvocablesStorage>();
    private readonly ICronTriggeredInvocablesStorage _cronTriggeredInvocablesStorage = Substitute.For<ICronTriggeredInvocablesStorage>();
    private readonly IScheduler _scheduler = Substitute.For<IScheduler>();

    private readonly InvocablesManager _sut;

    public InvocablesManagerTests()
    {
        _sut = new InvocablesManager(_eventTriggeredInvocablesStorage, _cronTriggeredInvocablesStorage, _scheduler);
    }

    [Fact]
    public void Ctor()
    {
        var invocablesManager = new InvocablesManager(_eventTriggeredInvocablesStorage, _cronTriggeredInvocablesStorage, _scheduler);
    }
}
