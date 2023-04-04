using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Models.Options;

namespace TagTool.Backend.Services;

public interface ICommonStoragePathProvider
{
    OneOf<string, None> GetPathForFile(ReadOnlySpan<char> fullName);

    OneOf<string, None> GetPathForFolder(ReadOnlySpan<char> fullName);
}

/// <summary>
///     This class manages structure of files stored by TagTool internally
/// </summary>
[UsedImplicitly]
public class CommonStoragePathProvider : ICommonStoragePathProvider
{
    // todo: change to IOptionSnapshot or something like that.
    private readonly CommonStorageOptions _options;
    private string FilesRoot => Path.Combine(_options.RootFolder, "Files");
    private string FoldersRoot => Path.Combine(_options.RootFolder, "Folders");

    public CommonStoragePathProvider(IOptions<CommonStorageOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(FilesRoot);
        Directory.CreateDirectory(FoldersRoot);
    }

    /// <summary>
    ///     Construct a path based on input path. The resulting path depends on a file extension.
    /// </summary>
    /// <param name="fullName">path to file</param>
    /// <returns>
    ///     Path in a following format:
    ///     [CommonStorageRootPath]/files/[Extensions]/[originalFileName]
    /// </returns>
    public OneOf<string, None> GetPathForFile(ReadOnlySpan<char> fullName)
    {
        var fileName = Path.GetFileName(fullName);
        var ext = Path.HasExtension(fullName) ? Path.GetExtension(fullName)[1..] : Path.GetExtension(fullName);

        if (fileName.IsEmpty || fileName.IsWhiteSpace()) return new None();

        var newFullName = Path.Combine(FilesRoot, ext.ToString(), fileName.ToString());

        var _ = Directory.CreateDirectory(Path.GetDirectoryName(newFullName)!);

        if (!File.Exists(newFullName))
        {
            return newFullName;
        }

        return new None();
    }

    /// <summary>
    ///     Construct a path based on input path.
    /// </summary>
    /// <returns>
    ///     Path in a following format:
    ///     [CommonStorageRootPath]/folders/[Extensions]/[originalFileName]
    /// </returns>
    public OneOf<string, None> GetPathForFolder(ReadOnlySpan<char> fullName)
        => Path.Combine(FoldersRoot, Path.GetFileName(Path.TrimEndingDirectorySeparator(fullName)).ToString());
}
