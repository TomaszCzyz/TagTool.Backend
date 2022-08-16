using Ganss.Text;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Services;

public class TagSearchService : Backend.TagSearchService.TagSearchServiceBase
{
    public override async Task FindTags(
        FindTagsRequest request,
        IServerStreamWriter<FindTagsReply> responseStream,
        ServerCallContext context)
    {
        var tags = await GetTags(request.PartialTagName, request.MaxReturn, context.CancellationToken);

        using var enumerator = tags.GetEnumerator();

        while (enumerator.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var tagName = enumerator.Current;
            await responseStream.WriteAsync(new FindTagsReply { TagName = tagName }, context.CancellationToken);
        }
    }

    public override async Task MatchTags(
        MatchTagsRequest request,
        IServerStreamWriter<MatchTagsReply> responseStream,
        ServerCallContext context)
    {
        var dict = request.PartialTagName.GetAllSubstrings();
        var ahoCorasick = new AhoCorasick(dict);

        var tagNames = await GetAllTags(ct: context.CancellationToken);

        foreach (var tagName in tagNames)
        {
            var wordMatches = ahoCorasick.Search(tagName).ToList();

            // todo: filter search results to exclude overlying substrings 
            if (wordMatches.Count == 0) continue;

            var matchTagsReply = new MatchTagsReply { TagName = tagName };
            foreach (var match in wordMatches)
            {
                matchTagsReply.MatchedParts.Add(new MatchedPart { StartIndex = match.Index, Length = match.Word.Length });
            }

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
        }
    }

    private static async Task<IEnumerable<string>> GetTags(string partialTagName, int requestMaxReturn, CancellationToken ct)
    {
        await using var db = new TagContext();

        var query = await db.Tags
            .AsNoTracking()
            .Select(tag => tag.Name)
            .Where(tagName => tagName.StartsWith(partialTagName))
            .OrderBy(static tagName => tagName)
            .Take(requestMaxReturn)
            .ToListAsync(cancellationToken: ct);

        return query;
    }

    // todo: adjust this method for batch processing for case when we have extremely large number of tags (what is extremely large?)
    private static async Task<IEnumerable<string>> GetAllTags(CancellationToken ct = default)
    {
        await using var db = new TagContext();

        var query = await db.Tags
            .AsNoTracking()
            .Select(tag => tag.Name)
            .ToListAsync(ct);

        return query;
    }
}

public static class StringExtensions // todo: optimize with span<char>
{
    public static string[] GetAllSubstrings(this string word)
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
}
