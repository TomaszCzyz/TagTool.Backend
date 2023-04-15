namespace TagTool.Backend.Models.Options;

public class CommonStorageOptions
{
    private static readonly string _defaultBasePath
        = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\TagTool\CommonStorage";

    public string Files { get; init; } = $@"{_defaultBasePath}\Files\";

    public string Directories { get; init; } = $@"{_defaultBasePath}\Folders\";
}
