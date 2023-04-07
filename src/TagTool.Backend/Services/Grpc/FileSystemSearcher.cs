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

    public override async Task Search(SearchRequest request, IServerStreamWriter<SearchReply> responseStream, ServerCallContext context)
    {
        IStreamRequest<string> streamReq = request.SearchTypeCase switch
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

        var asyncEnumerable = _mediator.CreateStream(streamReq, context.CancellationToken);

        try
        {
            await foreach (var fullName in asyncEnumerable.WithCancellation(context.CancellationToken))
            {
                if (context.CancellationToken.IsCancellationRequested) return;

                await responseStream.WriteAsync(new SearchReply { FullName = fullName }, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search was cancelled");
        }
    }
}
