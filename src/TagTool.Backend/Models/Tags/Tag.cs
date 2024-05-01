using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace TagTool.Backend.Models.Tags;

public interface ITagBase;

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

    public override string ToString() => FormattedName;
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

        var projected = new { tag.Id, tag.FormattedName };
        result = propertyValueFactory.CreatePropertyValue(projected, true);
        return true;
    }
}
