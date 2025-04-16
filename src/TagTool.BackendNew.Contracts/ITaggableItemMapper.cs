using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Contracts;

public interface ITaggableItemMapper
{
    public string ItemType { get; }

    public Type SelfType { get; }

    public TaggableItem MapFromString(string payload);

    public (string ItemType, string Payload) MapToString(TaggableItem item);
}
