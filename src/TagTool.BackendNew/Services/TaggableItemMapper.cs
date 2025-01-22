using System.Text.Json;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services;

public class TaggableItemMapper
{
    public TaggableItem MapToObj(string type, string taggableItem)
    {
        return type switch
        {
            "file" => JsonSerializer.Deserialize<TaggableFile.TaggableFile>(taggableItem, JsonSerializerOptions.Web)!,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public (string Type, string Payload) MapFromObj(TaggableItem item)
    {
        return item switch
        {
            TaggableFile.TaggableFile file => (Type: "file", Payload: JsonSerializer.Serialize(file, JsonSerializerOptions.Web)),
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };
    }
}
