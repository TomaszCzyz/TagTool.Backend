using System.Text.Json.Serialization;
using TagTool.BackendNew.Contracts.Broadcasting;

namespace TagTool.BackendNew.Contracts.Invocables.Common;

public abstract class PayloadWithChangedItems
{
    [JsonIgnore]
    public IEnumerable<ItemTagsChangedEvent> TaggableItems { get; set; } = [];
}
