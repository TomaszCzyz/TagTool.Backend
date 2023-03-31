using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands;

public class RenameFileResponse
{
    public string? ErrorMessage { get; init; }

    public bool IsRenamed => ErrorMessage is null;
}

[UsedImplicitly]
public class RenameFileRequest : IRequest<RenameFileResponse>
{
    public required string FullPath { get; init; }

    public required string NewFileName { get; init; }
}

[UsedImplicitly]
public class RenameFile : IRequestHandler<RenameFileRequest, RenameFileResponse>
{
    private readonly ILogger<RenameFile> _logger;
    private readonly TagToolDbContext _dbContext;

    public RenameFile(ILogger<RenameFile> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<RenameFileResponse> Handle(RenameFileRequest request, CancellationToken cancellationToken)
    {
        var oldFullPath = request.FullPath;
        var parentDir = Path.GetDirectoryName(oldFullPath)!;
        var newFullPath = Path.Combine(parentDir, request.NewFileName);

        var taggedItem = await _dbContext.TaggedItems.FirstOrDefaultAsync(
            item => item.ItemType == "file" && item.UniqueIdentifier == oldFullPath,
            cancellationToken: cancellationToken);

        if (taggedItem is null)
        {
            return RenameUntrackedFile(oldFullPath, newFullPath);
        }

        taggedItem.UniqueIdentifier = newFullPath;

        var entityEntry = _dbContext.TaggedItems.Update(taggedItem);

        try
        {
            File.Move(oldFullPath, newFullPath);
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
            return new RenameFileResponse { ErrorMessage = $"Unable to rename \"{Path.GetFileNameWithoutExtension(oldFullPath)}\"." };
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new RenameFileResponse();
    }

    private RenameFileResponse RenameUntrackedFile(string oldFullPath, string newFullPath)
    {
        try
        {
            File.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to rename untracked file {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new RenameFileResponse { ErrorMessage = $"Unable to rename \"{Path.GetFileNameWithoutExtension(oldFullPath)}\"." };
        }

        return new RenameFileResponse();
    }
}
