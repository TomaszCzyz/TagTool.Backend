using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace TagTool.BackendNew;

public static class PluginsHelper
{
    public static Assembly[] LoadedAssemblies { get; private set; } = [];

    static PluginsHelper()
    {
        var pluginsDir = Environment.GetEnvironmentVariable("PLUGINS_DIR");
        if (string.IsNullOrWhiteSpace(pluginsDir))
        {
            Log.Warning("PLUGINS_DIR is not set");
            return;
        }

        List<Assembly> loadedAssemblies = [];
        foreach (var dllFilePath in Directory.GetFiles(pluginsDir, "TagTool.BackendNew.TaggableItems.*.dll", SearchOption.AllDirectories))
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllFilePath);

            Log.Information("Loading plugin {AssemblyName} from file {DllFilePath}", assembly.FullName, dllFilePath);
            loadedAssemblies.Add(assembly);
        }

        LoadedAssemblies = loadedAssemblies.ToArray();
    }
}
