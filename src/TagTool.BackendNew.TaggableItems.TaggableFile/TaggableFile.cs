using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.TaggableItems.TaggableFile;

public class TaggableFile : TaggableItem, ITaggableItemType
{
    public static string TypeName { get; } = "TaggableFile_A8ABBA71";

    public required string Path { get; set; }
}
