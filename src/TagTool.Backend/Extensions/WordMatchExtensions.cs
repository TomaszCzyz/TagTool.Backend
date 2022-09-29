using Ganss.Text;

namespace TagTool.Backend.Extensions;

public static class WordMatchExtensions
{
    /// <summary>
    ///     Removes from collection words that match the same fragment of the original word.
    ///     Longer words have higher priority.
    /// </summary>
    /// <param name="wordMatches">output from AhoCorasick algorithm</param>
    /// <param name="originalWord">original work for reference</param>
    /// <returns>The longest, non-overlaying matched words</returns>
    public static IEnumerable<WordMatch> ExcludeOverlaying(this IEnumerable<WordMatch> wordMatches, string originalWord)
    {
        var flags = new bool[originalWord.Length];

        foreach (var match in wordMatches.OrderByDescending(match => match.Word.Length))
        {
            if (flags[match.Index]) continue;

            Array.Fill(flags, true, match.Index, match.Word.Length);
            yield return match;
        }
    }
}
