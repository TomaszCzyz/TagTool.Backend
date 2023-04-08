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

    // todo: I think I overdid it... maybe I do not need Func<> here, because ConcurrentBag is reference type, so updates will be visible here.. 
    public required Func<IReadOnlyCollection<string>> ExcludePathsAction { get; init; }

    public required bool IgnoreCase { get; init; }
}
