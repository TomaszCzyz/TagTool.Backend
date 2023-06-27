using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Models.Options;

namespace TagTool.Backend.Services;

public interface ICommonStoragePathProvider
{
    OneOf<string, None> GetPathForFile(string fileName);

    OneOf<string, None> GetPathForFolder(string fullName);
}

/// <summary>
///     This class manages structure of files stored by TagTool internally
/// </summary>
[UsedImplicitly]
public class CommonStoragePathProvider : ICommonStoragePathProvider
{
    private readonly ILogger<CommonStoragePathProvider> _logger;

    // todo: change to IOptionSnapshot or something like that.
    private readonly CommonStorageOptions _options;

    public CommonStoragePathProvider(ILogger<CommonStoragePathProvider> logger, IOptions<CommonStorageOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        Directory.CreateDirectory(_options.Files);
        Directory.CreateDirectory(_options.Directories);
    }

    /// <summary>
    ///     Construct a path based on input path. The resulting path depends on a file extension.
    /// </summary>
    /// <param name="fileName">path of a file</param>
    /// <returns>
    ///     Path in a following format:
    ///     [CommonStorageDirectoriesPath]/[Extension]/[originalFileName]
    /// </returns>
    public OneOf<string, None> GetPathForFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new None();
        }

        var ext = Path.HasExtension(fileName) ? Path.GetExtension(fileName.AsSpan())[1..] : "_noExtension";
        var newFileDir = Path.Join(_options.Files, ext);

        if (!TryCreateDir(newFileDir))
        {
            return new None();
        }

        return Path.Join(_options.Files, ext, fileName);
    }

    /// <summary>
    ///     Construct a path based on input path.
    /// </summary>
    /// <returns>
    ///     Path in a following format:
    ///     [CommonStorageFilesPath]/[originalDirectoryName]
    /// </returns>
    public OneOf<string, None> GetPathForFolder(string fullName)
        => Path.Join(_options.Directories, Path.GetFileName(Path.TrimEndingDirectorySeparator(fullName.AsSpan())));

    private bool TryCreateDir(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to create directory {PathFullName} in CommonStorage", path);
        }

        return false;
    }
}
