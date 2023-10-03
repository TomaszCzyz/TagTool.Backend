using TagTool.Backend.Jobs;
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

    public static void AddJobs(this IServiceCollection services, params Type[] scanMarkers)
    {
        // I create an instance of a job just to access properties...
        // maybe it could be done better with static abstract members, however it requires more manual
        // registration code and makes auto detecting jobs harder. 
        var jobs = scanMarkers
            .SelectMany(marker => marker.Assembly.ExportedTypes
                .Where(x => typeof(IJob).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }))
            .Select(type => (Type: type, Instance: (IJob)Activator.CreateInstance(type)!))
            .ToArray();

        foreach (var (marker, instance) in jobs)
        {
            services.AddKeyedScoped(typeof(IJob), instance.Id, marker);
        }

        var jobInfos = jobs
            .Select(tuple => new Jobs.JobInfo(tuple.Instance.Id, tuple.Instance.Description, tuple.Instance.AttributesDescriptions))
            .ToArray()
            .AsReadOnly();

        services.AddSingleton(jobInfos);
    }
}
