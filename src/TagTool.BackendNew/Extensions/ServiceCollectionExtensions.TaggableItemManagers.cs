using System.Reflection;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Helpers;
using TagTool.BackendNew.Services;

namespace TagTool.BackendNew.Extensions;

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
