using System.Text.Json.Serialization;

namespace TagTool.BackendNew.Contracts.Entities;

// ? complex ID with column 'TaggableItemType' (e.g. file) and 'CustomIdentifier' (e.g. path) to ensure uniqueness?
// public interface ITaggableItem
// {
//     Guid Id { get; set; }
//
//     ICollection<TagBase> Tags { get; }
// }

public interface ITaggableItemType
{
    static abstract string TypeName { get; }
}

public abstract class TaggableItem // : ITaggableItemType
{
    // public static string TypeName { get; } = "Unknown"; // TODO: ???

    public Guid Id { get; set; }

    [JsonIgnore]
    public ISet<TagBase> Tags { get; set; } = new HashSet<TagBase>();

}
