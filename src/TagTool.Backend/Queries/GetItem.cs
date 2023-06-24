using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemQuery : IQuery<OneOf<TaggedItemBase, ErrorResponse>>
{
    public required TaggableItem TaggableItem { get; init; }
}

[UsedImplicitly]
public class GetItem : IQueryHandler<GetItemQuery, OneOf<TaggedItemBase, ErrorResponse>>
{
    private readonly TagToolDbContext _dbContext;

    public GetItem(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggedItemBase, ErrorResponse>> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        TaggableItem? taggableItem = request.TaggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders.FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (taggableItem is null) return new ErrorResponse($"TaggableItem {request.TaggableItem} was not found in tagged items collection");

        return await _dbContext.TaggedItemsBase
            .Include(taggedItemBase => taggedItemBase.Tags)
            .Include(taggedItemBase => taggedItemBase.Item)
            .FirstAsync(taggedItemBase => taggedItemBase.Item.Id == taggableItem.Id, cancellationToken);
    }
}
