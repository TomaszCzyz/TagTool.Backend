using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Mappers;

namespace TagTool.Backend.Extensions;

public static class CollectionExtensions
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

    public static string?[] Names(this IEnumerable<TagBase> tagCollection) => tagCollection.Select(tag => tag.ToString()).ToArray();

    public static IEnumerable<Any> MapToDto(this IEnumerable<TagBase> tagBases) => tagBases.Select(TagMapper.MapToDto);
}
