using System.Reflection;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Services;

namespace TagTool.BackendNew.Extensions;

public static class TaggableItemsHelper
{
    public static Dictionary<Type, string> TaggableItemTypes { get; private set; } = new();

    public static void Initialize(Assembly[] assemblyMarkers)
    {
        TaggableItemTypes = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(ITaggableItemType).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .ToDictionary(
                type => type,
                type => type.GetProperty(nameof(ITaggableItemType.TypeName), BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string
                        ?? throw new InvalidOperationException($"No TypeName property found on {type.Name}"));
    }
}

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaggableItemManagers(this IServiceCollection services, Assembly[] assemblyMarkers)
    {
        services.AddSingleton<TaggableItemManagerDispatcher>();

        var taggableItemManagerTypes = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(ITaggableItemManagerBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Where(x => x != typeof(TaggableItemManagerDispatcher))
            .Select(type
                => (ItemType: type
                        .GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITaggableItemManager<>))
                        .GetGenericArguments()
                        .First(),
                    ManagerType: type));

        foreach (var taggableItemManagerType in taggableItemManagerTypes)
        {
            services.AddKeyedScoped(
                typeof(ITaggableItemManagerBase),
                TaggableItemsHelper.TaggableItemTypes[taggableItemManagerType.ItemType],
                taggableItemManagerType.ManagerType);
        }

        return services;
    }
}
