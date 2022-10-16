using System.Text;
using LiteDB;

namespace TagTool.Backend.Repositories;

public abstract class TaggedItemDto
{
    public int Id { get; init; }

    [BsonRef("Tags")]
    public List<TagDto> Tags { get; init; } = new();
}

public class FileDto : TaggedItemDto
{
    public required string FullPath { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FileDto));
        stringBuilder.Append(" { ");
        stringBuilder.Append("FullPath = ");
        stringBuilder.Append(FullPath);
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
}

public class FolderDto : TaggedItemDto
{
    public required string FullPath { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FolderDto));
        stringBuilder.Append(" { ");
        stringBuilder.Append("FullPath = ");
        stringBuilder.Append(FullPath);
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
}

public class TagDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public float HierarchyValue { get; set; }

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

    public static IEqualityComparer<TagDto> NameComparer { get; } = new NameEqualityComparer();
}
