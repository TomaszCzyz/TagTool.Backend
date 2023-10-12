using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Extensions;

public static class EnumerableExtensions
{
    public static void AddIfNotExists<T>(this ICollection<T> collection, T entity, Func<T?, bool>? predicate = null)
        where T : class?, new()
    {
        var exists = predicate != null ? collection.Any(predicate) : collection.Count > 0;
        if (!exists)
        {
            collection.Add(entity);
        }
    }

    public static string?[] Names(this IEnumerable<TagBase> tagCollection) => tagCollection.Select(tag => tag.FormattedName).ToArray();

    public static IEnumerable<T> CreateInstances<T>(this IEnumerable<Type> sourceTypes)
        => sourceTypes.Where(x => typeof(T).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .Cast<T>();
}
