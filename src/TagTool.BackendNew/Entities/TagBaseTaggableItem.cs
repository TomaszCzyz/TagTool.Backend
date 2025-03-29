using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Entities;

/// <summary>
///     'Join class' between <see cref="TagBase"/> and <see cref="TaggableItem" />.
///     It is used to track tagging/untagging of the items.
/// </summary>
public class TagBaseTaggableItem
{
    public int TagBaseId { get; set; }

    public Guid TaggableItemId { get; set; }
}
