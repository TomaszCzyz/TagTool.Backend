using System.Security;
using JetBrains.Annotations;
using MediatR;

namespace TagTool.Backend.Queries;

public class CanRenameFileResponse
{
    public string? Message { get; init; }

    public bool CanRename => Message is null;
}

public class CanRenameFileRequest : IRequest<CanRenameFileResponse>
{
    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class CanRenameFile : IRequestHandler<CanRenameFileRequest, CanRenameFileResponse>
{
    private readonly ILogger<CanRenameFile> _logger;

    private readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

    public CanRenameFile(ILogger<CanRenameFile> logger)
    {
        _logger = logger;
    }

    public Task<CanRenameFileResponse> Handle(CanRenameFileRequest request, CancellationToken cancellationToken)
    {
        if (Path.GetFileName(request.NewFullPath).IndexOfAny(_invalidFileNameChars) != -1)
        {
            return Task.FromResult(new CanRenameFileResponse { Message = "Filename contains forbidden characters." });
        }

        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(request.NewFullPath);
        }
        catch (SecurityException ex)
        {
            _logger.LogDebug(ex, "Cannot create/access a file at path {Path}, because of an exception", request.NewFullPath);
            return Task.FromResult(new CanRenameFileResponse { Message = "Cannot access specified path for security reasons." });
        }
        catch (PathTooLongException)
        {
            return Task.FromResult(
                new CanRenameFileResponse { Message = "Path is too long." });
        }
        catch (NotSupportedException)
        {
            return Task.FromResult(
                new CanRenameFileResponse { Message = "Filename cannot contain a colon (:)." });
        }

        if (fileInfo.Exists)
        {
            return Task.FromResult(new CanRenameFileResponse { Message = "File with given name already exists." });
        }

        return Task.FromResult(new CanRenameFileResponse());
    }
}
