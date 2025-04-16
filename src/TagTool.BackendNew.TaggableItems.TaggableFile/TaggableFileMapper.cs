using System.Text.Json;
using JetBrains.Annotations;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.TaggableItems.TaggableFile;

[UsedImplicitly]
public class TaggableFileMapper : ITaggableItemMapper
{
    public string ItemType { get; } = TaggableFile.TypeName;

    public Type SelfType { get; } = typeof(TaggableFile);

    public TaggableItem MapFromString(string payload)
        => JsonSerializer.Deserialize<TaggableFile>(payload) ?? throw new InvalidOperationException("Incorrect payload format.");

    public (string ItemType, string Payload) MapToString(TaggableItem item)
    {
        if (item is not TaggableFile taggableFile)
        {
            throw new ArgumentException("Item is not of type TaggableFile.", nameof(item));
        }

        var payload = JsonSerializer.Serialize(taggableFile);

        return (ItemType, payload);
    }
}
