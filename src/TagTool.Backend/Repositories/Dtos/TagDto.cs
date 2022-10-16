namespace TagTool.Backend.Repositories.Dtos;

public class TagDto
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public float HierarchyValue { get; set; }

    public static IEqualityComparer<TagDto> NameComparer { get; } = new NameEqualityComparer();

    private sealed class NameEqualityComparer : IEqualityComparer<TagDto>
    {
        public bool Equals(TagDto? x, TagDto? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(TagDto obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
