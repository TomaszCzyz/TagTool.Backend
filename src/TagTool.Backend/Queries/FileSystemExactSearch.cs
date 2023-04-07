using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;

namespace TagTool.Backend.Queries;

public class FileSystemExactSearchRequest : IStreamRequest<string>
{
    public required string Value { get; set; }

    public required string Root { get; set; }

    public required int Depth { get; set; }
}

[UsedImplicitly]
public class FileSystemExactSearch : IStreamRequestHandler<FileSystemExactSearchRequest, string>
{
    private readonly ILogger<FileSystemExactSearch> _logger;

    public FileSystemExactSearch(ILogger<FileSystemExactSearch> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<string> Handle(
        FileSystemExactSearchRequest request,
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
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName.EndsWith("c027196101.svgz")
            };

        foreach (var item in enumeration)
        {
            yield return item;
        }
    }
}
