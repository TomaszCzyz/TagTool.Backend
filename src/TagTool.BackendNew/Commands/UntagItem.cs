using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<TaggableItem, Error<string>>;

public class UntagItem : ICommand<Response>
{
    public required int TagId { get; init; }

    public required Guid ItemId { get; init; }
}

[UsedImplicitly]
public class UntagItemCommandHandler : ICommandHandler<UntagItem, Response>
{
    private readonly ILogger<UntagItem> _logger;
    private readonly ITagToolDbContextExtended _dbContext;

    public UntagItemCommandHandler(ILogger<UntagItem> logger, ITagToolDbContextExtended dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(UntagItem request, CancellationToken cancellationToken)
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

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, item);

        var isRemoved = item.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return isRemoved
            ? item
            : new Error<string>($"Unable to remove tag {tag} from item {item}, item might not be tagged with given tag.");
    }
}
