using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class UntagFolderChildrenRequest : ICommand<OneOf<string, ErrorResponse>>, IReversible
{
    public required string RootFolder { get; init; }

    public required TagBase Tag { get; init; }

    public int Depth { get; init; } = 1;

    public bool TagFilesOnly { get; init; } = true;

    public IReversible GetReverse()
        => new TagFolderChildrenRequest
        {
            RootFolder = RootFolder,
            Tag = Tag,
            Depth = Depth,
            TagFilesOnly = TagFilesOnly
        };
}

[UsedImplicitly]
public class UntagFolderChildren : ICommandHandler<UntagFolderChildrenRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<UntagFolderChildren> _logger;
    private readonly IMediator _mediator;

    public UntagFolderChildren(ILogger<UntagFolderChildren> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(UntagFolderChildrenRequest request, CancellationToken cancellationToken)
    {
        var dirInfo = new DirectoryInfo(request.RootFolder);
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = request.Depth > 1,
            MaxRecursionDepth = request.Depth,
            ReturnSpecialDirectories = false
        };

        var responses = new List<OneOf<TaggableItem, ErrorResponse>>();

        _logger.LogInformation(
            "Untagging items in folder {FolderPath} using enumeration options {@EnumerationOptions}",
            request.RootFolder,
            enumerationOptions);

        foreach (var info in dirInfo.EnumerateFileSystemInfos("*", enumerationOptions))
        {
            if (info is DirectoryInfo && request.TagFilesOnly) continue;

            var untagItemRequest = new UntagItemRequest
            {
                Tag = request.Tag,
                TaggableItem = info is FileInfo ? new TaggableFile { Path = info.FullName } : new TaggableFolder { Path = info.FullName }
            };

            var response = await _mediator.Send(untagItemRequest, cancellationToken);

            if (response.TryPickT1(out var errorResponse, out _))
            {
                _logger.LogInformation(
                    "Item {ItemFullName} was not untagged, because of an error {ErrorMessage}",
                    info.FullName,
                    errorResponse.Message);
            }

            responses.Add(response);
        }

        // todo: introduce aggregated error message or list of tagItem errors or something...
        return responses.Any(response => response.IsT0)
            ? "Some or all items were untagged"
            : new ErrorResponse("Even one item was not tagged.");
    }
}
