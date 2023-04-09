using System.Collections.Concurrent;
using MediatR;
using OneOf;

namespace TagTool.Backend.Queries;

public readonly struct CurrentlySearchDir
{
    public required string FullName { get; init; }
}

public abstract class FileSystemSearchRequestBase : IStreamRequest<OneOf<string, CurrentlySearchDir>>
{
    public required string Root { get; init; }

    public required int Depth { get; init; }

    public required ConcurrentBag<string> ExcludePaths { get; init; }

    public required bool IgnoreCase { get; init; }
}
