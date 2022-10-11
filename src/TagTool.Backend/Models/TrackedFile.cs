using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace TagTool.Backend.Models;

public class InternalProperties
{
    public bool IsExternal { get; set; }

    public int TimesSearched { get; set; }
}

public class TrackedFile
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required long Length { get; set; }

    public required string Path { get; set; }

    public ICollection<Tag> Tags { get; } = new List<Tag>();

    /// <summary>
    ///     Ctor for EF Core
    /// </summary>
    [UsedImplicitly]
    private TrackedFile()
    {
    }

    [SetsRequiredMembers]
    public TrackedFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new ArgumentException($"File with path {path} does not exists");
        }

        var fileInfo = new FileInfo(path);

        Name = fileInfo.Name;
        Length = fileInfo.Length;
        Path = fileInfo.DirectoryName!;
    }
}
