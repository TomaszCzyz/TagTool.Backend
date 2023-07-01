using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public class TagAssociations
{
    public int Id { get; set; }

    public required TagBase Tag { get; set; }

    public required List<AssociationDescription> Descriptions { get; set; }
}

public class AssociationDescription
{
    public int Id { get; set; }

    public int TagAssociationsId { get; set; }

    public TagBase Tag { get; set; } = null!;

    public AssociationType AssociationType { set; get; }
}

public enum AssociationType
{
    None = 0,
    Synonyms = 1,
    IsSubtype = 2
}
