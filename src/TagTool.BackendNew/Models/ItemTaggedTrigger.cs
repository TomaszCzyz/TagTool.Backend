using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.Models;

public class ItemTaggedTrigger : ITrigger
{
    private static ItemTaggedTrigger? _instance;

    public static ItemTaggedTrigger Instance => _instance ??= new ItemTaggedTrigger();

    private ItemTaggedTrigger()
    {
    }
}
