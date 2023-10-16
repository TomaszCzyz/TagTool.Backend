using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Actions;

/// <summary>
///     Base class for jobs performed on <see cref="TaggableItem"/>s
/// </summary>
/// <remarks>
///     Every job type should have unique <see cref="Id"/>.
///     It could be enforced by making JobId field static. However, <see cref="Id"/>
///     interface could not by used as type parameter then, what would make registration of a jobs
///     much more complex.
///     Do not forget to assign unique value to <see cref="IAction"/>
/// </remarks>
public interface IAction
{
    string Id { get; }

    string? Description { get; }

    IDictionary<string, string>? AttributesDescriptions { get; }

    ItemTypeTag[] ItemTypes { get; }

    Task<ActionResult> ExecuteOnSchedule(TagQuery tagQuery, Dictionary<string, string> data);

    Task<ActionResult> ExecuteByEvent(IEnumerable<Guid> itemIds, Dictionary<string, string> data);
}
