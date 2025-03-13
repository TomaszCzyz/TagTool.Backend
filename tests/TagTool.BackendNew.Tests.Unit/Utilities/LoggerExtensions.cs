using Microsoft.Extensions.Logging;
using NSubstitute;

namespace TagTool.BackendNew.Tests.Unit.Utilities;

public static class LoggerExtensions
{
    public static void AssertLog(this ILogger logger, LogLevel expectedLogLevel, string expectedMessage)
        => logger
            .Received(1)
            .Log(
                expectedLogLevel,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString() == expectedMessage),
                null,
                Arg.Any<Func<object, Exception?, string>>());
}
