using System.Diagnostics;
using System.IO.Enumeration;
using TagTool.Backend.Queries;

namespace TagTool.Backend.Services;

public interface ICustomFileSystemEnumerableFactory
{
    IEnumerable<(string FullPath, bool IsMatch)> Create(
        FileSystemSearchRequestBase requestBase,
        FileSystemEnumerable<(string FullPath, bool IsMatch)>.FindPredicate isMatch);
}

public class CustomFileSystemEnumerableFactory : ICustomFileSystemEnumerableFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public CustomFileSystemEnumerableFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IEnumerable<(string FullPath, bool IsMatch)> Create(
        FileSystemSearchRequestBase requestBase,
        FileSystemEnumerable<(string FullPath, bool IsMatch)>.FindPredicate isMatch)
    {
        var logger = _loggerFactory.CreateLogger(requestBase.GetType());

        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MaxRecursionDepth = requestBase.Depth
        };

        (string FullPath, bool IsMatch) FindTransform(ref FileSystemEntry entry) => (entry.ToFullPath(), isMatch(ref entry));

        return
            new FileSystemEnumerable<(string FullPath, bool IsMatch)>(requestBase.Root, FindTransform, options)
            {
                // always return directories to send search progress
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory || isMatch(ref entry),
                ShouldRecursePredicate =
                    (ref FileSystemEntry entry) =>
                    {
                        Debug.Assert(entry.IsDirectory, "entry.IsDirectory");
                        logger.LogInformation("Checking enumeration criteria for folder {EntryFullPath}", entry.ToFullPath());

                        var excludedPaths = requestBase.ExcludePathsAction.Invoke();
                        logger.LogInformation("Currently excluded paths: {Paths}", string.Join(",", excludedPaths));

                        foreach (var path in excludedPaths)
                        {
                            if (IsSubDirectory(entry, path.AsSpan()) || IsDirectoryEqual(entry, path.AsSpan()))
                            {
                                logger.LogInformation("Skipping enumerating of folder {EntryFullPath}", entry.ToFullPath());
                                return false;
                            }
                        }

                        return true;
                    }
            };
    }

    private static bool IsSubDirectory(FileSystemEntry entry, ReadOnlySpan<char> path)
    {
        // path:            C:\Users\xxx\Files\SubDir
        // dirOfPath:       C:\Users\xxx\Files\
        // fileNameOfPath:  SubDir
        //
        // entry.Directory = C:\Users\xxx\Files\SubDir\
        // entry.FileName = AnotherSubDir
        var dirOfPath = Path.GetDirectoryName(path);
        var fileNameOfPath = Path.GetFileName(path);

        return dirOfPath.StartsWith(entry.Directory) && fileNameOfPath.StartsWith(entry.FileName);
    }

    // todo: check if this method is necessary.. maybe IsSubDirectory is more general and it is enough
    private static bool IsDirectoryEqual(FileSystemEntry entry, ReadOnlySpan<char> path)
    {
        var dirOfPath = Path.GetDirectoryName(path);
        var fileNameOfPath = Path.GetFileName(path);

        return entry.Directory.SequenceEqual(dirOfPath) && entry.FileName.SequenceEqual(fileNameOfPath);
    }
}
