using System.Diagnostics;
using System.IO.Enumeration;
using TagTool.Backend.Extensions;
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

        return
            new FileSystemEnumerable<(string FullPath, bool IsMatch)>(requestBase.Root, FindTransform, options)
            {
                // always return directories to send search progress
                ShouldIncludePredicate =
                    (ref FileSystemEntry entry) => isMatch(ref entry) || (entry.IsDirectory && !IsExcluded(requestBase.ExcludePaths, entry)),
                ShouldRecursePredicate =
                    (ref FileSystemEntry entry) =>
                    {
                        Debug.Assert(entry.IsDirectory, "entry.IsDirectory");
                        logger.LogDebug("Checking enumeration criteria for folder {EntryFullPath}", entry.ToFullPath());

                        var excludedPaths = requestBase.ExcludePaths;

                        if (!IsExcluded(excludedPaths, entry))
                        {
                            return true;
                        }

                        logger.LogDebug("Skipping enumerating of folder {EntryFullPath}", entry.ToFullPath());
                        return false;
                    }
            };

        (string FullPath, bool IsMatch) FindTransform(ref FileSystemEntry entry) => (entry.ToFullPath(), isMatch(ref entry));
    }

    private static bool IsExcluded(IEnumerable<string> excludedPaths, FileSystemEntry entry)
    {
        // foreach cannot be converted to LINQ-expression as this would require ref struct (FileSystemEntry) to be captured
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var path in excludedPaths)
        {
            if (entry.IsSubdirectoryOf(path.AsSpan()))
            {
                return true;
            }
        }

        return false;
    }
}
