using System.Security;
using JetBrains.Annotations;
using MediatR;

namespace TagTool.Backend.Queries;

public class CanRenameFolderResponse
{
    public string? Message { get; init; }

    public bool CanRename => Message is null;
}

public class CanRenameFolderRequest : IQuery<CanRenameFolderResponse>
{
    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class CanRenameFolder : IQueryHandler<CanRenameFolderRequest, CanRenameFolderResponse>
{
    private readonly ILogger<CanRenameFolder> _logger;

    private readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

    public CanRenameFolder(ILogger<CanRenameFolder> logger)
    {
        _logger = logger;
    }

    public Task<CanRenameFolderResponse> Handle(CanRenameFolderRequest request, CancellationToken cancellationToken)
    {
        if (Path.GetDirectoryName(request.NewFullPath)?.IndexOfAny(_invalidPathChars) != -1)
        {
            return Task.FromResult(new CanRenameFolderResponse { Message = "Folder path contains forbidden characters." });
        }

        DirectoryInfo directoryInfo;
        try
        {
            directoryInfo = new DirectoryInfo(request.NewFullPath);
        }
        catch (SecurityException ex)
        {
            _logger.LogDebug(ex, "Cannot create/access a path {Path}, because of an exception", request.NewFullPath);
            return Task.FromResult(new CanRenameFolderResponse { Message = "Cannot access specified path for security reasons." });
        }
        catch (PathTooLongException)
        {
            return Task.FromResult(new CanRenameFolderResponse { Message = "Path is too long." });
        }

        if (directoryInfo.Exists)
        {
            return Task.FromResult(new CanRenameFolderResponse { Message = "Folder with given name already exists." });
        }

        return Task.FromResult(new CanRenameFolderResponse());
    }
}
