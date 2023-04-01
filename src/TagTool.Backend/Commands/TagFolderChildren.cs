using JetBrains.Annotations;
using MediatR;

namespace TagTool.Backend.Commands;

public class TagFolderChildrenResponse
{
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorMessage is null;
}

public class TagFolderChildrenRequest : IRequest<TagFolderChildrenResponse>
{
    public required string RootFolder { get; init; }

    public required string TagName { get; init; }

    public int Depth { get; init; } = 1;

    public bool TagFilesOnly { get; init; } = true;
}

[UsedImplicitly]
public class TagFolderChildren : IRequestHandler<TagFolderChildrenRequest, TagFolderChildrenResponse>
{
    private readonly ILogger<TagFolderChildren> _logger;
    private readonly IMediator _mediator;

    public TagFolderChildren(ILogger<TagFolderChildren> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<TagFolderChildrenResponse> Handle(TagFolderChildrenRequest request, CancellationToken cancellationToken)
    {
        var dirInfo = new DirectoryInfo(request.RootFolder);
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = request.Depth > 1,
            MaxRecursionDepth = request.Depth,
            ReturnSpecialDirectories = false
        };

        var responses = new List<TagItemResponse>();

        _logger.LogInformation(
            "Tagging items in folder {FolderPath} using enumeration options {@EnumerationOptions}",
            request.RootFolder,
            enumerationOptions);

        foreach (var info in dirInfo.EnumerateFileSystemInfos("*.*", enumerationOptions))
        {
            if (info is DirectoryInfo && request.TagFilesOnly) continue;

            var tagItemRequest = new TagItemRequest
            {
                TagName = request.TagName,
                ItemType = info is FileInfo ? "file" : "folder",
                Identifier = info.FullName
            };

            var response = await _mediator.Send(tagItemRequest, cancellationToken);

            if (response.ErrorMessage is not null)
            {
                _logger.LogInformation(
                    "Item {ItemFullName} was not tagged, because of an error {ErrorMessage}",
                    info.FullName,
                    response.ErrorMessage);
            }

            responses.Add(response);
        }

        // todo: introduce aggregated error message or list of tagItem errors or something...
        return responses.Any(response => response.ErrorMessage is null)
            ? new TagFolderChildrenResponse()
            : new TagFolderChildrenResponse { ErrorMessage = "Even one item was not tagged." };
    }
}
