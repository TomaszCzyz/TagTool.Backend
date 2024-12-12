using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace TagTool.BackendNew.Entities;

public interface ITag
{
    string Text { get; }

    ICollection<TaggableItem> TaggedItems { get; set; }
}

[DebuggerDisplay("{Text}")]
public class TagBase : ITag
{
    public Guid Id { get; set; }

    public required string Text { get; init; }

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

public sealed class TagBaseDeconstructPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [UnscopedRef] out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is not TagBase tag)
        {
            result = null!;
            return false;
        }

        var projected = new { tag.Id, tag.Text };
        result = propertyValueFactory.CreatePropertyValue(projected, true);
        return true;
    }
}
