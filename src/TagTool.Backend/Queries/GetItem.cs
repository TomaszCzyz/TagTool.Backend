using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemQuery : IQuery<TaggableItem?>
{
    public required TaggableItem TaggableItem { get; init; }
}

[UsedImplicitly]
public class GetItem : IQueryHandler<GetItemQuery, TaggableItem?>
{
    private readonly ITagToolDbContext _dbContext;

    public GetItem(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaggableItem?> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        return request.TaggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles
                    .Include(file => file.Tags)
                    .FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders
                    .Include(folder => folder.Tags)
                    .FirstOrDefaultAsync(folder => folder.Path == taggableFolder.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
    }
}
