using System.Text.Json.Serialization;
using TagTool.BackendNew.Broadcasting;

namespace TagTool.BackendNew.Invocables.Common;

public abstract class PayloadWithChangedItems
{
    [JsonIgnore]
    public IEnumerable<ItemTagsChangedEvent> TaggableItems { get; set; } = [];
}
