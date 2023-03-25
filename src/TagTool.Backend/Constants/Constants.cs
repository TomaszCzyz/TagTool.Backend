namespace TagTool.Backend.Constants;

public static class Constants
{
    private const string ApplicationName = "TagToolBackend";

    private static readonly string _localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static readonly string BasePath = Path.Join(_localAppDataPath, ApplicationName, "Sqlite");

    // todo: move socket file location to AppData/.. 
    public static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "socket.tmp");

    public static readonly string DbPath = Path.Join(BasePath, "TagTool.db");

    public static readonly string LogsDbPath = Path.Combine(BasePath, "Logs", "log.db");
}
