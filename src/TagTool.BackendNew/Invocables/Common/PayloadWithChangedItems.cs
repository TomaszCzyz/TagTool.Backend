using TagTool.BackendNew.Broadcasting;

namespace TagTool.BackendNew.Invocables.Common;

public abstract class PayloadWithChangedItems
{
    public IEnumerable<ItemTagsChangedEvent> TaggableItems { get; set; }
}
