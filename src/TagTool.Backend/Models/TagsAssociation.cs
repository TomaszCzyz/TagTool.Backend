using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public class TagsAssociation
{
    public int Id { get; set; }

    public required TagBase FirstTag { get; set; }

    public required TagBase SecondTag { get; set; }

    public AssociationType AssociationType { set; get; }
}

public enum AssociationType
{
    None = 0,
    Synonyms = 1,
    IsSubtype = 2
}
