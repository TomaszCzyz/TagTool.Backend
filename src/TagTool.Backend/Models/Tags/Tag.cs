using System.Diagnostics;

namespace TagTool.Backend.Models.Tags;

public interface ITagBase
{
}

[DebuggerDisplay("{FormattedName}")]
public abstract class TagBase : ITagBase, IHasTimestamps
{
    public int Id { get; set; }

    // The cleaner way to do this would be abstract get-only property...
    // however (sqlite's?) migration validation throws an error
    // 'No backing field could be found for property 'TagBase.FormattedName' and the property does not have a setter.'
    public string FormattedName { get; protected set; } = null!;

    public ICollection<TaggableItem> TaggedItems { set; get; } = new List<TaggableItem>();

    public DateTime? Added { get; set; }

    public DateTime? Deleted { get; set; }

    public DateTime? Modified { get; set; }
}
