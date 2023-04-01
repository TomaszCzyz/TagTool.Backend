using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands;

public class MoveFolderResponse
{
    public string? ErrorMessage { get; init; }

    public bool IsMoved => ErrorMessage is null;
}

public class MoveFolderRequest : IRequest<MoveFolderResponse>
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class MoveFolder : IRequestHandler<MoveFolderRequest, MoveFolderResponse>
{
    private readonly ILogger<MoveFolder> _logger;
    private readonly TagToolDbContext _dbContext;

    public MoveFolder(ILogger<MoveFolder> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<MoveFolderResponse> Handle(MoveFolderRequest request, CancellationToken cancellationToken)
    {
        var (oldFullPath, newFullPath) = (request.OldFullPath, request.NewFullPath);

        if (Directory.Exists(newFullPath))
        {
            return new MoveFolderResponse { ErrorMessage = "Folder with the same filename already exists in the destination location." };
        }

        var taggedItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == "folder" && item.UniqueIdentifier == oldFullPath, cancellationToken);

        if (taggedItem is null)
        {
            return MoveUntrackedFolder(oldFullPath, newFullPath);
        }

        taggedItem.UniqueIdentifier = newFullPath;

        var entityEntry = _dbContext.TaggedItems.Update(taggedItem);

        try
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to move folder from {OldPath} to {NewPath}. Rolling back TaggedItem {@TaggedItem} update",
                oldFullPath,
                newFullPath,
                taggedItem);

            entityEntry.Entity.UniqueIdentifier = oldFullPath;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new MoveFolderResponse { ErrorMessage = $"Unable to move a folder from \"{oldFullPath}\" to \"{newFullPath}\"." };
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new MoveFolderResponse();
    }

    private MoveFolderResponse MoveUntrackedFolder(string oldFullPath, string newFullPath)
    {
        try
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to move untracked folder from {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new MoveFolderResponse { ErrorMessage = $"Unable to move a folder from \"{oldFullPath}\" to \"{newFullPath}\"." };
        }

        return new MoveFolderResponse();
    }
}
