using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class TagFolderChildrenRequest : ICommand<OneOf<string, ErrorResponse>>, IReversible
{
    public required string RootFolder { get; init; }

    public required TagBase Tag { get; init; }

    public int Depth { get; init; } = 1;

    public bool TagFilesOnly { get; init; } = true;

    public IReversible GetReverse()
        => new UntagFolderChildrenRequest
        {
            RootFolder = RootFolder,
            Tag = Tag,
            Depth = Depth,
            TagFilesOnly = TagFilesOnly
        };
}

[UsedImplicitly]
public class TagFolderChildren : ICommandHandler<TagFolderChildrenRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<TagFolderChildren> _logger;
    private readonly IMediator _mediator;

    public TagFolderChildren(ILogger<TagFolderChildren> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(TagFolderChildrenRequest request, CancellationToken cancellationToken)
    {
        var dirInfo = new DirectoryInfo(request.RootFolder);
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = request.Depth > 1,
            MaxRecursionDepth = request.Depth,
            ReturnSpecialDirectories = false
        };

        var responses = new List<OneOf<TaggedItem, ErrorResponse>>();

        _logger.LogInformation(
            "Tagging items in folder {FolderPath} using enumeration options {@EnumerationOptions}",
            request.RootFolder,
            enumerationOptions);

        foreach (var info in dirInfo.EnumerateFileSystemInfos("*", enumerationOptions))
        {
            if (info is DirectoryInfo && request.TagFilesOnly) continue;

            var tagItemRequest = new TagItemRequest
            {
                Tag = request.Tag,
                ItemType = info is FileInfo ? "file" : "folder",
                Identifier = info.FullName
            };

            var response = await _mediator.Send(tagItemRequest, cancellationToken);

            if (response.TryPickT1(out var errorResponse, out _))
            {
                _logger.LogInformation(
                    "Item {ItemFullName} was not tagged, because of an error {ErrorMessage}",
                    info.FullName,
                    errorResponse.Message);
            }

            responses.Add(response);
        }

        // todo: introduce aggregated error message or list of tagItem errors or something...
        return responses.Any(response => response.IsT0)
            ? "Some or all items were tagged"
            : new ErrorResponse("Even one item was not tagged.");
    }
}
