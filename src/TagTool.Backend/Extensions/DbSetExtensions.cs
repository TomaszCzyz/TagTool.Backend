using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace TagTool.Backend.Extensions;

public static class DbSetExtensions
{
    public static T AddIfNotExists<T>(
        this DbSet<T> dbSet,
        T entity,
        Expression<Func<T, bool>>? predicate = null) where T : class
    {
        T? entry = null;
        if (predicate != null)
        {
            entry = dbSet.FirstOrDefault(predicate);
        }

        return entry ?? dbSet.Add(entity).Entity;
    }
}
