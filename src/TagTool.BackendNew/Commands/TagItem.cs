using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<TaggableItem, Error<string>>;

public class TagItem : ICommand<Response>
{
    public required int TagId { get; init; }

    public required Guid ItemId { get; init; }
}

[UsedImplicitly]
public class TagItemRequestHandler : ICommandHandler<TagItem, Response>
{
    private readonly ILogger<TagItem> _logger;
    private readonly ITagToolDbContext _dbContext;

    public TagItemRequestHandler(ILogger<TagItem> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(TagItem request, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags.FindAsync([request.TagId], cancellationToken);

        if (tag is null)
        {
            return new Error<string>("Tag not found");
        }

        var item = await _dbContext.TaggableItems.FindAsync([request.ItemId], cancellationToken);

        if (item is null)
        {
            return new Error<string>("Item not found");
        }

        if (item.Tags.Contains(tag))
        {
            return new Error<string>("Item already contains tag");
        }

        _logger.LogInformation("Tagging item {@TaggedItem} with tag {@Tags}", item, tag);
        item.Tags.Add(tag);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return item;
    }
}
