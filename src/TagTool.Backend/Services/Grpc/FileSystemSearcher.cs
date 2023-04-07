using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Grpc.Core;
using MediatR;
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
        // todo: it does not take to account case when MoveNext() return false... NullPointerException will be thrown 
        await requestStream.MoveNext(context.CancellationToken);
        var firstRequest = requestStream.Current;
        // var streamRequest = MapRequest(firstRequest);
        var excludedPaths = new ConcurrentBag<string>();
        var streamRequest = new FileSystemRegexSearchRequest
        {
            Depth = firstRequest.Depth,
            Root = firstRequest.Root,
            ExcludePathsAction = () => excludedPaths,
            Value = new Regex(
                firstRequest.Regex.Value,
                RegexOptions.NonBacktracking | (firstRequest.Regex.IgnoreCase ? RegexOptions.IgnoreCase : 0),
                TimeSpan.FromSeconds(firstRequest.Regex.TimeoutInSeconds))
        };

        var asyncEnumerable = _mediator.CreateStream(streamRequest, context.CancellationToken);

        var task = Task.Run(async () =>
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var newExcludedPaths = requestStream.Current.ExcludedPaths;
                foreach (var path in newExcludedPaths)
                {
                    if (excludedPaths.Contains(path)) continue;

                    excludedPaths.Add(path);
                }
            }
        });

        try
        {
            await foreach (var fullName in asyncEnumerable.WithCancellation(context.CancellationToken))
            {
                await responseStream.WriteAsync(new SearchReply { FullName = fullName }, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search was cancelled");
        }

        await task.WaitAsync(context.CancellationToken);
    }

    private IStreamRequest<string> MapRequest(SearchRequest request)
    {
        return request.SearchTypeCase switch
        {
            SearchRequest.SearchTypeOneofCase.Exact =>
                new FileSystemExactSearchRequest
                {
                    Depth = request.Depth,
                    Value = request.Exact.Value,
                    Root = request.Root
                },
            SearchRequest.SearchTypeOneofCase.Regex =>
                new FileSystemRegexSearchRequest
                {
                    Depth = request.Depth,
                    Root = request.Root,
                    Value = new Regex(
                        request.Regex.Value,
                        RegexOptions.NonBacktracking | (request.Regex.IgnoreCase ? RegexOptions.IgnoreCase : 0),
                        TimeSpan.FromSeconds(request.Regex.TimeoutInSeconds))
                }
        };
    }
}
