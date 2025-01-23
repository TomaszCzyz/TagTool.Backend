using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.TaggableFile;

public class TaggableFile : TaggableItem, ITaggableItemType
{
    public static string TypeName { get; } = "TaggableFile_A8ABBA71";

    public required string Path { get; set; }
}
