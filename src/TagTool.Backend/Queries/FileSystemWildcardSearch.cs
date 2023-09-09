using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class FileSystemWildcardSearchRequest : FileSystemSearchRequestBase
{
    public required string Value { get; init; }
}

[UsedImplicitly]
public class FileSystemWildcardSearch : IStreamRequestHandler<FileSystemWildcardSearchRequest, OneOf<string, CurrentlySearchDir>>
{
    private readonly ILogger<FileSystemWildcardSearch> _logger;
    private readonly ICustomFileSystemEnumerableFactory _systemEnumerableFactory;

    public FileSystemWildcardSearch(ILogger<FileSystemWildcardSearch> logger, ICustomFileSystemEnumerableFactory systemEnumerableFactory)
    {
        _logger = logger;
        _systemEnumerableFactory = systemEnumerableFactory;
    }

    public async IAsyncEnumerable<OneOf<string, CurrentlySearchDir>> Handle(
        FileSystemWildcardSearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file system wildcard search with params {@Request}", request);

        bool IsMatch(ref FileSystemEntry entry) => FileSystemName.MatchesSimpleExpression(request.Value.AsSpan(), entry.FileName, request.IgnoreCase);

        var enumeration = _systemEnumerableFactory.Create(request, IsMatch);

        var (matchesCounter, dirCounter) = (0, 0);
        await foreach (var (fullPath, isMatch) in enumeration.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (isMatch)
            {
                matchesCounter++;
                yield return fullPath;
            }
            else
            {
                dirCounter++;
                yield return new CurrentlySearchDir { FullName = fullPath };
            }
        }

        _logger.LogInformation("Search ended with {MatchesCount} matches found in {DirCount} directories", matchesCounter, dirCounter);
    }
}
