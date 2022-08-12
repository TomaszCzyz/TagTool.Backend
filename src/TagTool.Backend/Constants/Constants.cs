namespace TagTool.Backend.Constants;

public static class Constants
{
    private const Environment.SpecialFolder LocalApplicationData = Environment.SpecialFolder.LocalApplicationData;

    public const string ApplicationName = "TagToolBackend";

    public static readonly string SocketPath = Path.Combine(Path.GetTempPath(), "socket.tmp");

    public static readonly string LocalAppDataDir = Environment.GetFolderPath(LocalApplicationData);

    public static readonly string DbDirPath = Path.Join(LocalAppDataDir, ApplicationName);

    public static readonly string DbPath = Path.Join(LocalAppDataDir, "TagTool.db");

    public static readonly string LogsDbPath = Path.Join(LocalAppDataDir, @"Logs\log.db");
}
