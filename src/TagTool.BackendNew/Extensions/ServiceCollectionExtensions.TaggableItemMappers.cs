using System.Reflection;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaggableMappers(this IServiceCollection services, Assembly[] assemblyMarkers)
    {
        var taggableItemMappers = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(ITaggableItemMapper).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false });

        foreach (var taggableItemMapper in taggableItemMappers)
        {
            services.AddSingleton(typeof(ITaggableItemMapper), taggableItemMapper);
        }

        return services;
    }
}
