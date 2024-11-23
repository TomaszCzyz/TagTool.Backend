using TagTool.Backend.Actions;
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
            tagFromDtoMappers.AddRange(marker.Assembly.ExportedTypes.CreateInstances<ITagFromDtoMapper>());
            tagToDtoMappers.AddRange(marker.Assembly.ExportedTypes.CreateInstances<ITagToDtoMapper>());
        }

        services.AddSingleton<IReadOnlyCollection<ITagFromDtoMapper>>(tagFromDtoMappers);
        services.AddSingleton<IReadOnlyCollection<ITagToDtoMapper>>(tagToDtoMappers);
    }

    public static void AddJobs(this IServiceCollection services, params Type[] scanMarkers)
    {
        // I create an instance of a job just to access properties...
        // maybe it could be done better with static abstract members, however it requires more manual
        // registration code and makes auto detecting jobs harder.
        var jobs = scanMarkers
            .SelectMany(marker => marker.Assembly.ExportedTypes
                .Where(x => typeof(IAction).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }))
            .Select(type => (Type: type, Instance: (IAction)Activator.CreateInstance(type)!))
            .ToArray();

        foreach (var (marker, instance) in jobs)
        {
            services.AddKeyedScoped(typeof(IAction), instance.Id, marker);
        }

        var jobInfos = jobs
            .Select(tuple =>
                new Actions.ActionInfo(
                    tuple.Instance.Id,
                    tuple.Instance.Description,
                    tuple.Instance.AttributesDescriptions,
                    tuple.Instance.ItemTypes))
            .ToArray()
            .AsReadOnly();

        services.AddSingleton(jobInfos);
    }
}
