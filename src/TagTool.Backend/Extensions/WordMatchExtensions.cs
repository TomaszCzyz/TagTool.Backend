using Ganss.Text;

namespace TagTool.Backend.Extensions;

public static class WordMatchExtensions
{
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
