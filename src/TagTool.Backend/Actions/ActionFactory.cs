using System.Collections.ObjectModel;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Actions;

public record ActionInfo(string Id, string? Description, IDictionary<string, string>? AttributesDescriptions, ItemTypeTag[] ItemTypes);

public interface IActionFactory
{
    IAction? Create(string actionId);

    IReadOnlyCollection<ActionInfo> GetAvailableActions();
}

public class ActionFactory : IActionFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ReadOnlyCollection<ActionInfo> _actionInfos;

    public ActionFactory(IServiceProvider serviceProvider, ReadOnlyCollection<ActionInfo> actionInfos)
    {
        _serviceProvider = serviceProvider;
        _actionInfos = actionInfos;
    }

    public IAction? Create(string actionId)
    {
        var serviceScope = _serviceProvider.CreateScope();
        var t = serviceScope.ServiceProvider.GetKeyedService<IAction>(actionId);
        return t;
    }

    public IReadOnlyCollection<ActionInfo> GetAvailableActions()
    {
        return _actionInfos;
    }
}
