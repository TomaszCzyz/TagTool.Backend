using System.Diagnostics;
using System.Text.Json.Serialization;

namespace TagTool.BackendNew.Contracts.Entities;

// TODO: add _AddedBy_ property
[DebuggerDisplay("{Text}")]
public class TagBase : ITag
{
    public int Id { get; set; }

    public required string Text { get; init; }

    [JsonIgnore]
    public ICollection<TaggableItem> TaggedItems { set; get; } = new List<TaggableItem>();

    public override string ToString() => Text;

    protected bool Equals(TagBase other) => Text == other.Text;

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((TagBase)obj);
    }

    public override int GetHashCode() => Text.GetHashCode();
}
