using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace TagTool.Backend.Models.Taggable;

public class InternalProperties
{
    public bool IsExternal { get; set; }

    public int TimesSearched { get; set; }
}

public class TrackedFile : ITaggable
{
    public int Id { get; set; }

    public required string FullPath { get; set; }

    public string Name => Path.GetFileName(FullPath);

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

        FullPath = Path.GetFullPath(path);
    }

    public Task<bool> Tag(string tagName) => throw new NotImplementedException();

    public Task<bool> Untag(string tagName) => throw new NotImplementedException();
}
