namespace TagTool.Backend.Extensions;

public static class StringExtensions // todo: optimize with span<char>
{
    public static string[] Substrings(this string word)
    {
        var substrings = new string[((1 + word.Length) * word.Length) / 2];

        var counter = 0;
        for (var substringLength = 1; substringLength <= word.Length; ++substringLength)
        {
            for (var startIndex = 0; startIndex <= word.Length - substringLength; startIndex++)
            {
                substrings[counter] = word.Substring(startIndex, substringLength);
                counter++;
            }
        }

        return substrings;
    }

    public static bool ContainsPath(this ReadOnlySpan<char> path, ReadOnlySpan<char> parentDir, ReadOnlySpan<char> dirName)
    {
        var pathDirectory = Path.GetDirectoryName(path);
        var pathFileName = Path.GetFileName(path);

        return pathDirectory.SequenceEqual(parentDir) && pathFileName.SequenceEqual(dirName) || parentDir.StartsWith(path);
    }
}
