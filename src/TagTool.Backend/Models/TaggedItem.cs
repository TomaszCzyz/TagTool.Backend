namespace TagTool.Backend.Models;

public class TaggedItemBase
{
    public int Id { get; set; }

    public required TaggableItem Item { get; set; }

    public ICollection<TagBase> Tags { get; set; } = new List<TagBase>();
}

public abstract class TaggableItem
{
    public Guid Id { get; set; }
}

public class TaggableFile : TaggableItem
{
    public required string Path { get; set; }

    private bool Equals(TaggableFile other) => Path == other.Path;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((TaggableFile)obj);
    }

    public override int GetHashCode() => Path.GetHashCode();
}

public class TaggableFolder : TaggableItem
{
    public required string Path { get; set; }

    private bool Equals(TaggableFolder other) => Path == other.Path;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((TaggableFolder)obj);
    }

    public override int GetHashCode() => Path.GetHashCode();
}
