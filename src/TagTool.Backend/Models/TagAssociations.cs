using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public class TagSynonymsGroup
{
    public int Id { get; set; }

    // todo: add unique key
    public required string Name { get; set; }

    public required ICollection<TagBase> Synonyms { get; set; }

    public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Synonyms)} count: {Synonyms.Count}";
}

public class TagsHierarchy
{
    public int Id { get; set; }

    public required TagSynonymsGroup ParentGroup { get; set; }

    public required ICollection<TagSynonymsGroup> ChildGroups { get; set; }
}
