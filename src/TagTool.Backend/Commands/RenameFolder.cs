using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class RenameFolderRequest : IRequest<OneOf<string, ErrorResponse>>
{
    public required string FullPath { get; init; }

    public required string NewFolderName { get; init; }
}

[UsedImplicitly]
public class RenameFolder : IRequestHandler<RenameFolderRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<RenameFolder> _logger;
    private readonly TagToolDbContext _dbContext;

    public RenameFolder(ILogger<RenameFolder> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(RenameFolderRequest request, CancellationToken cancellationToken)
    {
        var oldFullPath = request.FullPath;
        var parentDir = Directory.GetParent(oldFullPath)?.FullName
                        ?? throw new ArgumentException("parent directory is null", nameof(request));
        var newFullPath = Path.Combine(parentDir, request.NewFolderName);

        var taggedItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == "folder" && item.UniqueIdentifier == oldFullPath, cancellationToken);

        if (taggedItem is null)
        {
            return RenameUntrackedFolder(oldFullPath, newFullPath);
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
                "Unable to rename {OldPath} to {NewPath}. Rolling back TaggedItem {@TaggedItem} update",
                taggedItem.UniqueIdentifier,
                newFullPath,
                taggedItem);

            entityEntry.Entity.UniqueIdentifier = oldFullPath;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new ErrorResponse($"Unable to rename \"{oldFullPath}\".");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return newFullPath;
    }

    private OneOf<string, ErrorResponse> RenameUntrackedFolder(string oldFullPath, string newFullPath)
    {
        try
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to rename untracked folder {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new ErrorResponse($"Unable to rename \"{oldFullPath}\"." );
        }

        return newFullPath;
    }
}
