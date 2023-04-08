using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class FileSystemRegexSearchRequest : FileSystemSearchRequestBase
{
    public required string Pattern { get; init; }
}

[UsedImplicitly]
public class FileSystemRegexSearch : IStreamRequestHandler<FileSystemRegexSearchRequest, OneOf<string, CurrentlySearchDir>>
{
    private readonly ILogger<FileSystemRegexSearch> _logger;
    private readonly ICustomFileSystemEnumerableFactory _systemEnumerableFactory;

    public FileSystemRegexSearch(ILogger<FileSystemRegexSearch> logger, ICustomFileSystemEnumerableFactory systemEnumerableFactory)
    {
        _logger = logger;
        _systemEnumerableFactory = systemEnumerableFactory;
    }

    public async IAsyncEnumerable<OneOf<string, CurrentlySearchDir>> Handle(
        FileSystemRegexSearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file system regex search with params {@Request}", request);

        var regexOptions = RegexOptions.NonBacktracking | (request.IgnoreCase ? RegexOptions.IgnoreCase : 0);
        var regex = new Regex(request.Pattern, regexOptions, TimeSpan.FromSeconds(3));

        bool IsMatch(ref FileSystemEntry entry) => regex.IsMatch(entry.FileName);

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
