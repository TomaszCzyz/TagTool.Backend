﻿namespace TagTool.Extensions;

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
}
