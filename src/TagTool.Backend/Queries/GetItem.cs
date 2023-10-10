using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class GetItemQuery : IQuery<OneOf<TaggableItem, ErrorResponse>>
{
    public required TaggableItem TaggableItem { get; init; }
}

[UsedImplicitly]
public class GetItem : IQueryHandler<GetItemQuery, OneOf<TaggableItem, ErrorResponse>>
{
    private readonly ITagToolDbContext _dbContext;

    public GetItem(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggableItem, ErrorResponse>> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        TaggableItem? taggableItem = request.TaggableItem switch
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

        return taggableItem
               ?? (OneOf<TaggableItem, ErrorResponse>)new ErrorResponse(
                   $"TaggableItem {request.TaggableItem} was not found in tagged items collection.");
    }
}
