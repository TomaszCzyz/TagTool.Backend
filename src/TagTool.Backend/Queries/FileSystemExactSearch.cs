﻿using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class FileSystemExactSearchRequest : FileSystemSearchRequestBase
{
    public required string Value { get; init; }
}

[UsedImplicitly]
public class FileSystemExactSearch : IStreamRequestHandler<FileSystemExactSearchRequest, OneOf<string, CurrentlySearchDir>>
{
    private readonly ILogger<FileSystemExactSearch> _logger;
    private readonly ICustomFileSystemEnumerableFactory _systemEnumerableFactory;

    public FileSystemExactSearch(ILogger<FileSystemExactSearch> logger, ICustomFileSystemEnumerableFactory systemEnumerableFactory)
    {
        _logger = logger;
        _systemEnumerableFactory = systemEnumerableFactory;
    }

    public async IAsyncEnumerable<OneOf<string, CurrentlySearchDir>> Handle(
        FileSystemExactSearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting file system search for an exact match with params {@Request}", request);

        bool IsMatch(ref FileSystemEntry entry) => entry.FileName.Contains(request.Value.AsSpan(), StringComparison.Ordinal);

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
