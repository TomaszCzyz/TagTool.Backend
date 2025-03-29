using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.DbContexts;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.TaggableItems.TaggableFile;

[UsedImplicitly]
public class TaggableFileManager : ITaggableItemManager<TaggableFile>
{
    private readonly ILogger<TaggableFileManager> _logger;
    private readonly ITagToolDbContext _dbContext;

    public TaggableFileManager(ILogger<TaggableFileManager> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<TaggableItem?> GetItem(TaggableFile item, CancellationToken cancellationToken)
    {
        return await _dbContext
            .Set<TaggableFile>()
            .FirstOrDefaultAsync(file => file.Path == item.Path, cancellationToken);
    }

    public async Task<TaggableItem> GetOrAddItem(TaggableFile item, CancellationToken cancellationToken)
    {
        var taggableFile = await _dbContext
            .Set<TaggableFile>()
            .FirstOrDefaultAsync(file => file.Path == item.Path, cancellationToken);

        if (taggableFile is not null)
        {
            _logger.LogDebug("TaggableFile {@TaggableFile} already exists", item);
            return taggableFile;
        }

        _logger.LogInformation("Creating new TaggableFile {@TaggableFile}", item);
        item.Id = Guid.CreateVersion7();
        var entityEntry = _dbContext.TaggableItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity;
    }
}
