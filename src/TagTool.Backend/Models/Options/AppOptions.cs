namespace TagTool.Backend.Models.Options;

public class AppOptions
{
    public required string UserConfigFilePath { get; init; } = Path.Join(Constants.Constants.BasePath, "userConfig.json");
}
