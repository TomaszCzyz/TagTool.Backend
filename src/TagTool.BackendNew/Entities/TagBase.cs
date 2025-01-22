using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Serilog.Core;
using Serilog.Events;
using TagTool.BackendNew.Common;

namespace TagTool.BackendNew.Entities;

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

public static class TagBaseExtensions
{
    public static Tag ToDto(this TagBase tag) => new() { Id = tag.Id, Text = tag.Text };

    public static IEnumerable<Tag> ToDtos(this IEnumerable<TagBase> tags) => tags.Select(t => t.ToDto());
}
