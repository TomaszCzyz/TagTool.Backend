using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Tests.Unit.Utilities;

public class TestInvocablePayload : PayloadWithQuery;

public class TestInvocable : ICronTriggeredInvocable<TestInvocablePayload>
{
    public TestInvocablePayload Payload { get; set; } = null!;
    public Task Invoke() => throw new NotImplementedException();
}
