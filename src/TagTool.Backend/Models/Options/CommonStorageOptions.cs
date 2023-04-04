namespace TagTool.Backend.Models.Options;

public class CommonStorageOptions
{
    public required string RootFolder { get; init; }
        = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\TagTool\CommonStorage";
}
