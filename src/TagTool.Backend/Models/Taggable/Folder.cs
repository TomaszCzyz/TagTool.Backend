namespace TagTool.Backend.Models.Taggable;

public class Folder : ITaggable
{
    private readonly string _fullPath = null!;

    public required string FullPath
    {
        get => _fullPath;
        init
        {
            if (!Directory.Exists(value))
            {
                throw new ArgumentException($"Folder with path {value} does not exists");
            }

            _fullPath = Path.GetFullPath(value);
        }
    }

    public string UniqueIdentifier => _fullPath;
}
