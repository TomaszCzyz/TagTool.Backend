using System.IO.Enumeration;

namespace TagTool.Backend.Extensions;

public static class FileSystemEntryExtensions
{
    /// <summary>
    /// This wrapper is created because <see cref="FileSystemEntry"/> struct is not unit testable.
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="path"></param>
    /// <returns>true - is given path is subdirectory of an entry. This method does not checks if dir exists.</returns>
    public static bool IsSubdirectoryOf(this FileSystemEntry entry, ReadOnlySpan<char> path)
        => path.ContainsPath(entry.Directory, entry.FileName);
}
