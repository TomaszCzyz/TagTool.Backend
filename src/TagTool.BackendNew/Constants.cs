namespace TagTool.BackendNew;

public static class Constants
{
    private const string ApplicationName = "TagTool";

    private static readonly string _localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static readonly string BasePath = Path.Join(_localAppDataPath, ApplicationName, "BackendNew");

    // todo: move socket file location to AppData/..
    public static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "socket.tmp");

    public static readonly string DbPath = Path.Join(BasePath, "TagTool.db");
}
