namespace TagTool.Backend.Extensions;

public static class SetExtensions
{
    public static bool AddMany<T>(this ISet<T> set, IEnumerable<T> items)
    {
        var result = false;
        foreach (var item in items)
        {
            var isAdded = set.Add(item);
            if (isAdded)
            {
                result = true;
            }
        }

        return result;
    }
}
