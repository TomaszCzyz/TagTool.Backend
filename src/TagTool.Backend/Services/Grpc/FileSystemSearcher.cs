using System.Collections.Concurrent;
using Grpc.Core;
using MediatR;
using OneOf;
using TagTool.Backend.Queries;

namespace TagTool.Backend.Services.Grpc;

public class FileSystemSearcher : SearchService.SearchServiceBase
{
    private readonly ILogger<FileSystemSearcher> _logger;
    private readonly IMediator _mediator;

    public FileSystemSearcher(ILogger<FileSystemSearcher> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public override async Task Search(
        IAsyncStreamReader<SearchRequest> requestStream,
        IServerStreamWriter<SearchReply> responseStream,
        ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken)) return;
        var firstRequest = requestStream.Current;

        var streamRequest = MapToSearchRequest(firstRequest);
        var excludedPaths = streamRequest.ExcludePaths;

        // listen to new messages and update ExcludedPaths collection
        var cts = new CancellationTokenSource();
        var task = Task.Run(
            async () =>
            {
                try
                {
                    await foreach (var request in requestStream.ReadAllAsync(cts.Token))
                    {
                        var newExcludedPaths = request.ExcludedPaths;
                        foreach (var path in newExcludedPaths)
                        {
                            if (excludedPaths.Contains(path)
                                || excludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.Ordinal)))
                            {
                                continue;
                            }

                            _logger.LogInformation("Adding excluded path {FullPath} to paths {Paths}", path, string.Join(",", excludedPaths));

                            excludedPaths.Add(path);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("The operation was canceled");
                }
            },
            cts.Token);

        var asyncEnumerable = _mediator.CreateStream(streamRequest, context.CancellationToken);

        await SendReplies(asyncEnumerable, responseStream, context);

        // search ended, so we do not need listen for more updates
        cts.Cancel();
        try
        {
            await task.WaitAsync(context.CancellationToken);
        }
        catch
        {
            // ignored
        }

        task.Dispose();
    }

    private async Task SendReplies(
        IAsyncEnumerable<OneOf<string, CurrentlySearchDir>> asyncEnumerable,
        IAsyncStreamWriter<SearchReply> responseStream,
        ServerCallContext context)
    {
        try
        {
            await foreach (var info in asyncEnumerable.WithCancellation(context.CancellationToken))
            {
                var searchReply = info.Match(
                    s => new SearchReply { FullName = s },
                    dir => new SearchReply { CurrentlySearchDir = dir.FullName });

                await responseStream.WriteAsync(searchReply, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search was cancelled");
        }
    }

    private static FileSystemSearchRequestBase MapToSearchRequest(SearchRequest request)
    {
        return request.SearchTypeCase switch
        {
            SearchRequest.SearchTypeOneofCase.Exact
                => new FileSystemExactSearchRequest
                {
                    Depth = request.Depth,
                    Value = request.Exact.Substring,
                    Root = request.Root,
                    ExcludePaths = new ConcurrentBag<string>(request.ExcludedPaths),
                    IgnoreCase = request.IgnoreCase
                },
            SearchRequest.SearchTypeOneofCase.Wildcard
                => new FileSystemWildcardSearchRequest
                {
                    Depth = request.Depth,
                    Value = request.Wildcard.Pattern,
                    Root = request.Root,
                    ExcludePaths = new ConcurrentBag<string>(request.ExcludedPaths),
                    IgnoreCase = request.IgnoreCase
                },
            SearchRequest.SearchTypeOneofCase.Regex
                => new FileSystemRegexSearchRequest
                {
                    Depth = request.Depth,
                    Pattern = request.Regex.Pattern,
                    Root = request.Root,
                    ExcludePaths = new ConcurrentBag<string>(request.ExcludedPaths),
                    IgnoreCase = request.IgnoreCase
                },
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Unknown search type")
        };
    }
}
