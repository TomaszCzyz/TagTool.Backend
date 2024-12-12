namespace TagTool.BackendNew.Options;

public class AppOptions
{
    public required string UserConfigFilePath { get; init; } = Path.Join(Constants.BasePath, "userConfig.json");
}
