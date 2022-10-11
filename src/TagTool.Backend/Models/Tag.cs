using TagTool.Backend.Models.Taggable;

namespace TagTool.Backend.Models;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public float HierarchyValue { get; set; }

    public List<Group> Groups { get; set; } = null!;

    public ICollection<ITaggable> TaggedItems { get; } = new List<ITaggable>();
}
