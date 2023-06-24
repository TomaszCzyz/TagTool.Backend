using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class DoesItemExistsQuery : IQuery<bool>
{
    public required TaggableItem TaggableItem { get; init; }
}

[UsedImplicitly]
public class DoesItemExists : IQueryHandler<DoesItemExistsQuery, bool>
{
    private readonly TagToolDbContext _dbContext;

    public DoesItemExists(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DoesItemExistsQuery request, CancellationToken cancellationToken)
    {
        TaggableItem? taggableItem = request.TaggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders.FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        return taggableItem is not null;
    }
}
