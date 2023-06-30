using TagTool.Backend.Mappers;

namespace TagTool.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddTagDtoMappers(this IServiceCollection services, params Type[] scanMarkers)
    {
        var tagFromDtoMappers = new List<ITagFromDtoMapper>();
        var tagToDtoMappers = new List<ITagToDtoMapper>();

        foreach (var marker in scanMarkers)
        {
            tagFromDtoMappers.AddRange(
                marker.Assembly.ExportedTypes
                    .Where(x => typeof(ITagFromDtoMapper).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
                    .Select(Activator.CreateInstance)
                    .Cast<ITagFromDtoMapper>()
            );

            tagToDtoMappers.AddRange(
                marker.Assembly.ExportedTypes
                    .Where(x => typeof(ITagToDtoMapper).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
                    .Select(Activator.CreateInstance)
                    .Cast<ITagToDtoMapper>()
            );
        }

        services.AddSingleton(tagFromDtoMappers as IReadOnlyCollection<ITagFromDtoMapper>);
        services.AddSingleton(tagToDtoMappers as IReadOnlyCollection<ITagToDtoMapper>);
    }
}
