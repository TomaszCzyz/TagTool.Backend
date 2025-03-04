namespace TagTool.BackendNew.Contracts;

public interface ITrigger;

public class ItemTaggedTrigger : ITrigger
{
    private static ItemTaggedTrigger? _instance;

    public static ItemTaggedTrigger Instance => _instance ??= new ItemTaggedTrigger();

    private ItemTaggedTrigger()
    {
    }
}

public class CronTrigger : ITrigger
{
    public required string CronExpression { get; init; }
}
