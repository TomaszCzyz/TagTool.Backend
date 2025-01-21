using System.Text.Json;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services;

public class TaggableItemMapper
{
    public TaggableItem Map(string type, string taggableItem)
    {
        return type switch
        {
            "file" => JsonSerializer.Deserialize<TaggableFile.TaggableFile>(taggableItem, JsonSerializerOptions.Web)!,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
