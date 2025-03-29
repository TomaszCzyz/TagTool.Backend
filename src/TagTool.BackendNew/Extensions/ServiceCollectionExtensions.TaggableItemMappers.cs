using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaggableMappers(this IServiceCollection services)
    {
        var taggableItemMappers = typeof(Program).Assembly.ExportedTypes
            .Where(x => typeof(ITaggableItemMapper).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false });

        foreach (var taggableItemMapper in taggableItemMappers)
        {
            services.AddSingleton(taggableItemMapper);
        }

        return services;
    }
}
