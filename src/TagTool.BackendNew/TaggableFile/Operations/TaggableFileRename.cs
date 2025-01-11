using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.TaggableFile.Operations;

using Response = OneOf<Success, Error<string>>;

public class TaggableFileRename : ITaggableFileOperation<Response>
{
    public static string Name { get; } = "file:rename";

    public Guid ItemId { get; set; }

    public required string NewName { get; init; }
}

public class TaggableFileRenameOperationHandler : IRequestHandler<TaggableFileRename, Response>
{
    private readonly ILogger<TaggableFileRenameOperationHandler> _logger;
    private readonly ITagToolDbContext _dbContext;

    public TaggableFileRenameOperationHandler(ILogger<TaggableFileRenameOperationHandler> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(TaggableFileRename request, CancellationToken cancellationToken)
    {
        var taggedItem = await _dbContext
            .Set<TaggableFile>()
            .FirstOrDefaultAsync(file => file.Id == request.ItemId, cancellationToken);

        if (taggedItem is null)
        {
            return new Error<string>("Could not find tagged file.");
        }

        var oldFullPath = taggedItem.Path;
        var parentDir = Path.GetDirectoryName(oldFullPath)!;
        var newFullPath = Path.Combine(parentDir, request.NewName);

        taggedItem.Path = newFullPath;
        var entityEntry = _dbContext.Set<TaggableFile>().Update(taggedItem);

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
            return new Error<string>($"Unable to rename \"{Path.GetFileNameWithoutExtension(oldFullPath)}\".");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new Success();
    }
}
