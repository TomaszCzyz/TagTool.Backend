using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands;

public class MoveFileResponse
{
    public string? ErrorMessage { get; init; }

    public bool IsMoved => ErrorMessage is null;
}

[UsedImplicitly]
public class MoveFileRequest : IRequest<MoveFileResponse>
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class MoveFile : IRequestHandler<MoveFileRequest, MoveFileResponse>
{
    private readonly ILogger<MoveFile> _logger;

    public MoveFile(ILogger<MoveFile> logger)
    {
        _logger = logger;
    }

    public async Task<MoveFileResponse> Handle(MoveFileRequest request, CancellationToken cancellationToken)
    {
        if (!Path.Exists(Path.GetDirectoryName(request.NewFullPath)))
        {
            return new MoveFileResponse { ErrorMessage = "Specified destination folder does not exists." };
        }

        if (File.Exists(request.NewFullPath))
        {
            return new MoveFileResponse { ErrorMessage = "File with the same filename already exists in the destination location." };
        }

        await using var db = new TagContext();

        var taggedItem = await db.TaggedItems.FirstOrDefaultAsync(
            item => item.ItemType == "file" && item.UniqueIdentifier == request.OldFullPath,
            cancellationToken: cancellationToken);

        if (taggedItem is null)
        {
            return MoveUntrackedFile(request.OldFullPath, request.NewFullPath);
        }

        taggedItem.UniqueIdentifier = request.NewFullPath;

        var entityEntry = db.TaggedItems.Update(taggedItem);

        try
        {
            File.Move(request.OldFullPath, request.NewFullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to move file from {OldPath} to {NewPath}. Rolling back TaggedItem {@TaggedItem} update",
                request.OldFullPath,
                request.NewFullPath,
                taggedItem);

            entityEntry.Entity.UniqueIdentifier = request.OldFullPath;

            await db.SaveChangesAsync(cancellationToken);
            return new MoveFileResponse { ErrorMessage = $"Unable to move a file from \"{request.OldFullPath}\" to \"{request.NewFullPath}\"." };
        }

        await db.SaveChangesAsync(cancellationToken);
        return new MoveFileResponse();
    }

    private MoveFileResponse MoveUntrackedFile(string oldFullPath, string newFullPath)
    {
        try
        {
            File.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to move untracked file from {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new MoveFileResponse { ErrorMessage = $"Unable to move a file from \"{oldFullPath}\" to \"{newFullPath}\"." };
        }

        return new MoveFileResponse();
    }
}
