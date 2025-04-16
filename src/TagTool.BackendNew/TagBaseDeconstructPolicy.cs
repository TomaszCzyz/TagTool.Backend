using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew;

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
