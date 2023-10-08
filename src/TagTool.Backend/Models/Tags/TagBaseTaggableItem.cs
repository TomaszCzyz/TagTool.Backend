namespace TagTool.Backend.Models.Tags;

/// <summary>
///     'Join class' between <see cref="TagBase"/> and <see cref="TaggableItem" />.
///     It is used to track tagging/untagging of the items.
/// </summary>
public class TagBaseTaggableItem
{
    public int TagBaseId { get; set; }

    public Guid TaggableItemId { get; set; }
}
