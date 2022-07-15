using JetBrains.Annotations;

namespace TagTool.Models;

public class File
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public long Length { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public string? Location { get; set; }

    public IList<Tag> Tags { get; set; } = null!;

    [UsedImplicitly]
    public File()
    {
    }

    public File(string name, long length, DateTime? dateModified, DateTime? dateCreated, string? location, IList<Tag> tags)
    {
        Name = name;
        Length = length;
        DateModified = dateModified;
        DateCreated = dateCreated;
        Location = location;
        Tags = tags;
    }

    public File(FileInfo fileInfo, Tag tag)
    {
        Name = fileInfo.Name;
        Length = fileInfo.Length;
        DateCreated = fileInfo.CreationTime;
        DateModified = fileInfo.LastWriteTime;
        Location = fileInfo.DirectoryName;
        Tags = new List<Tag> {tag};
    }

    public static explicit operator File(FileInfo fileInfo) =>
        new(fileInfo.Name, fileInfo.Length, fileInfo.CreationTime, fileInfo.LastWriteTime, fileInfo.DirectoryName, new List<Tag>());
}
