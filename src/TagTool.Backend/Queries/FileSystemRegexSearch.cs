using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using MediatR;

namespace TagTool.Backend.Queries;

public class FileSystemRegexSearchRequest : IStreamRequest<string>
{
    public required Regex Value { get; init; }

    public required string Root { get; init; }

    public required int Depth { get; init; }

    public Func<IReadOnlyCollection<string>> ExcludePathsAction { get; init; }
}

[UsedImplicitly]
public class FileSystemRegexSearch : IStreamRequestHandler<FileSystemRegexSearchRequest, string>
{
    private readonly ILogger<FileSystemRegexSearch> _logger;

    public FileSystemRegexSearch(ILogger<FileSystemRegexSearch> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<string> Handle(
        FileSystemRegexSearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MaxRecursionDepth = request.Depth
        };

        string FindTransform(ref FileSystemEntry entry) => entry.ToFullPath();

        var enumeration =
            new FileSystemEnumerable<string>(request.Root, FindTransform, options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && request.Value.IsMatch(entry.FileName),
                ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                {
                    Debug.Assert(entry.IsDirectory, "entry.IsDirectory");
                    _logger.LogInformation("Checking enumeration criteria for folder {EntryFullPath}", entry.ToFullPath());

                    var excludedPaths = request.ExcludePathsAction.Invoke();
                    _logger.LogInformation("Currently excluded paths: {Paths}", string.Join(",", excludedPaths));
                    foreach (var path in excludedPaths)
                    {
                        var dirOfPath = Path.GetDirectoryName(path.AsSpan());
                        var fileNameOfPath = Path.GetFileName(path.AsSpan());

                        if (!entry.Directory.SequenceEqual(dirOfPath) || !entry.FileName.SequenceEqual(fileNameOfPath)) continue;

                        _logger.LogInformation("Skipping enumerating of folder {EntryFullPath}", entry.ToFullPath());
                        return false;
                    }

                    return true;
                }
            };

        var counter = 0;
        await foreach (var item in enumeration.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            counter++;
            yield return item;
        }

        _logger.LogInformation("Search ended with {Count} file entries yielded", counter);
    }
}
