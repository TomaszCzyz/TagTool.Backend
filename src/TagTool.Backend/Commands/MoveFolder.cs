using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class MoveFolderRequest : IRequest<OneOf<string, ErrorResponse>>
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class MoveFolder : IRequestHandler<MoveFolderRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<MoveFolder> _logger;
    private readonly TagToolDbContext _dbContext;

    public MoveFolder(ILogger<MoveFolder> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(MoveFolderRequest request, CancellationToken cancellationToken)
    {
        var (oldFullPath, newFullPath) = (request.OldFullPath, request.NewFullPath);

        if (Directory.Exists(newFullPath))
        {
            return new ErrorResponse("Folder with the same filename already exists in the destination location.");
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
            return new ErrorResponse($"Unable to move a folder from \"{oldFullPath}\" to \"{newFullPath}\".");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return newFullPath;
    }

    private OneOf<string, ErrorResponse> MoveUntrackedFolder(string oldFullPath, string newFullPath)
    {
        try
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to move untracked folder from {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new ErrorResponse($"Unable to move a folder from \"{oldFullPath}\" to \"{newFullPath}\".");
        }

        return newFullPath;
    }
}
