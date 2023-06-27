using System.IO.Enumeration;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Options;

namespace TagTool.Backend.Services;

public record CommonStorageInfo(string Path, string? SimilarFiles);

public interface ICommonStorage
{
    OneOf<CommonStorageInfo, ErrorResponse> GetPath(string fullName, bool overwrite);
}

[UsedImplicitly]
public class CommonStorage : ICommonStorage
{
    private readonly ILogger<CommonStorage> _logger;
    private readonly CommonStorageOptions _commonStorageOptions;
    private readonly ICommonStoragePathProvider _commonStoragePathProvider;

    public CommonStorage(
        ILogger<CommonStorage> logger,
        ICommonStoragePathProvider commonStoragePathProvider,
        IOptions<CommonStorageOptions> commonStorageOptions)
    {
        _commonStoragePathProvider = commonStoragePathProvider;
        _logger = logger;
        _commonStorageOptions = commonStorageOptions.Value;
    }

    public OneOf<CommonStorageInfo, ErrorResponse> GetPath(string fullName, bool overwrite)
        => Directory.Exists(fullName) ? CanStoreFolder(fullName, overwrite) : GetPathForFile(fullName, overwrite);

    private OneOf<CommonStorageInfo, ErrorResponse> CanStoreFolder(string fullName, bool overwrite)
    {
        var oneOf = _commonStoragePathProvider.GetPathForFolder(Path.GetFileName(fullName));

        if (!oneOf.TryPickT0(out var newFullPath, out _))
        {
            return new ErrorResponse($"Unable to get path in Common Storage for {fullName}");
        }

        if (!overwrite && Directory.Exists(newFullPath))
        {
            return new ErrorResponse("File with the same name already exists. Use flag 'overwrite' to replace it");
        }

        return new CommonStorageInfo(newFullPath, null);
    }

    private OneOf<CommonStorageInfo, ErrorResponse> GetPathForFile(string fullName, bool overwrite)
    {
        var oneOf = _commonStoragePathProvider.GetPathForFile(Path.GetFileName(fullName));

        if (!oneOf.TryPickT0(out var newFullPath, out _))
        {
            return new ErrorResponse($"Unable to get path in Common Storage for {fullName}");
        }

        if (!overwrite && File.Exists(newFullPath))
        {
            return new ErrorResponse("File with the same name already exists. Use flag 'overwrite' to replace it");
        }

        var similarFiles = SimilarFiles(fullName);

        return similarFiles.Match(
            paths => new CommonStorageInfo(newFullPath, string.Join(",", paths)),
            _ => new CommonStorageInfo(newFullPath, null));
    }

    private OneOf<string[], None> SimilarFiles(string fullName)
    {
        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(fullName);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to initialize FileInfo for file {FileFullName} when trying to with similar files", fullName);
            return new None();
        }

        var searchInFiles = SearchInFiles(fileInfo);
        var searchInFolders = SearchInFolders(fileInfo);

        return searchInFiles.Union(searchInFolders).ToArray();
    }

    private IEnumerable<string> SearchInFiles(FileInfo fileInfo)
    {
        var options = new EnumerationOptions { IgnoreInaccessible = true };

        string FindTransform(ref FileSystemEntry entry) => entry.ToFullPath();
        bool ShouldInclude(ref FileSystemEntry entry) => entry.Length == fileInfo.Length;

        var rootFolder = Path.Join(_commonStorageOptions.Files, Path.GetExtension(fileInfo.Name));

        var fileSystemEnumerable = new FileSystemEnumerable<string>(rootFolder, FindTransform, options)
        {
            ShouldIncludePredicate = ShouldInclude
        };

        return fileSystemEnumerable;
    }

    // todo: it can take a while... change logic to background process?... 
    private IEnumerable<string> SearchInFolders(FileInfo fileInfo)
    {
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MaxRecursionDepth = 5
        };

        string FindTransform(ref FileSystemEntry entry) => entry.ToFullPath();
        bool ShouldInclude(ref FileSystemEntry entry) => entry.Length == fileInfo.Length;

        var rootFolder = _commonStorageOptions.Directories;

        var fileSystemEnumerable = new FileSystemEnumerable<string>(rootFolder, FindTransform, options)
        {
            ShouldIncludePredicate = ShouldInclude
        };

        return fileSystemEnumerable;
    }
}
