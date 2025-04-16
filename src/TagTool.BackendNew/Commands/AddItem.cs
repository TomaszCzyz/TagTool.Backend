using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.Services;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<TaggableItem, Error<string>>;

public class AddItem : ICommand<Response>
{
    public required string ItemType { get; init; }

    public required string ItemArgs { get; init; }
}

[UsedImplicitly]
public class AddItemCommandHandler : ICommandHandler<AddItem, Response>
{
    private readonly ILogger<AddItem> _logger;
    private readonly TaggableItemMapper _taggableItemMapper;
    private readonly TaggableItemManagerDispatcher _taggableItemManager;

    public AddItemCommandHandler(
        ILogger<AddItem> logger,
        TaggableItemMapper taggableItemMapper,
        TaggableItemManagerDispatcher taggableItemManager)
    {
        _logger = logger;
        _taggableItemMapper = taggableItemMapper;
        _taggableItemManager = taggableItemManager;
    }

    public async Task<Response> Handle(AddItem request, CancellationToken cancellationToken)
    {
        var taggableItem = _taggableItemMapper.MapFromString(request.ItemType, request.ItemArgs);
        var existingItem = await _taggableItemManager.GetItem(taggableItem, cancellationToken);

        if (existingItem is not null)
        {
            return new Error<string>("Item already exists.");
        }

        _logger.LogInformation("Adding new taggable item {@TaggableItem}", taggableItem);
        var item = await _taggableItemManager.GetOrAddItem(taggableItem, cancellationToken);

        return item;
    }
}
