namespace TagTool.Backend.Models.Taggable;

public class File : ITaggable
{
    private readonly string _fullPath = null!;

    public required string FullPath
    {
        get => _fullPath;
        init
        {
            if (!System.IO.File.Exists(value))
            {
                throw new ArgumentException($"File with path {value} does not exists");
            }

            _fullPath = Path.GetFullPath(value);
        }
    }
}
