using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class RenameFileRequest : ICommand<OneOf<string, ErrorResponse>>, IReversible
{
    public required string FullPath { get; init; }

    public required string NewFileName { get; init; }

    public IReversible GetReverse()
        => new RenameFileRequest { NewFileName = Path.GetFileName(FullPath), FullPath = Path.Combine(Path.GetDirectoryName(FullPath)!, NewFileName) };
}

[UsedImplicitly]
public class RenameFile : ICommandHandler<RenameFileRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<RenameFile> _logger;
    private readonly ITagToolDbContext _dbContext;

    public RenameFile(ILogger<RenameFile> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(RenameFileRequest request, CancellationToken cancellationToken)
    {
        var oldFullPath = request.FullPath;
        var parentDir = Path.GetDirectoryName(oldFullPath)!;
        var newFullPath = Path.Combine(parentDir, request.NewFileName);

        var taggedItem = await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == oldFullPath, cancellationToken);

        if (taggedItem is null)
        {
            return RenameUntrackedFile(oldFullPath, newFullPath);
        }

        taggedItem.Path = newFullPath;

        var entityEntry = _dbContext.TaggableFiles.Update(taggedItem);

        try
        {
            File.Move(oldFullPath, newFullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to rename {OldPath} to {NewPath}. Rolling back TaggedItem {@TaggedItem} update",
                taggedItem.Path,
                newFullPath,
                taggedItem);

            entityEntry.Entity.Path = oldFullPath;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new ErrorResponse($"Unable to rename \"{Path.GetFileNameWithoutExtension(oldFullPath)}\".");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return newFullPath;
    }

    private OneOf<string, ErrorResponse> RenameUntrackedFile(string oldFullPath, string newFullPath)
    {
        try
        {
            File.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to rename untracked file {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new ErrorResponse($"Unable to rename \"{Path.GetFileNameWithoutExtension(oldFullPath)}\".");
        }

        return newFullPath;
    }
}
