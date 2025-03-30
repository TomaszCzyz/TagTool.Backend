using System.Reflection;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Helpers;

public static class TaggableItemsHelper
{
    public static Dictionary<Type, string> TaggableItemTypes { get; private set; } = new();

    public static void Initialize(Assembly[] assemblyMarkers)
    {
        TaggableItemTypes = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(ITaggableItemType).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .ToDictionary(
                type => type,
                type => type.GetProperty(nameof(ITaggableItemType.TypeName), BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string
                        ?? throw new InvalidOperationException($"No TypeName property found on {type.Name}"));
    }
}
