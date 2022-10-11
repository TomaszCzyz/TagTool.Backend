// using Ganss.Text;
// using Grpc.Core;
// using Microsoft.EntityFrameworkCore;
// using TagTool.Backend.DbContext;
// using TagTool.Backend.Extensions;
//
// namespace TagTool.Backend.Services;
//
// public class TagSearchService : Backend.TagSearchService.TagSearchServiceBase
// {
//     public override async Task FindTags(
//         FindTagsRequest request,
//         IServerStreamWriter<FoundTagReply> responseStream,
//         ServerCallContext context)
//     {
//         var tags = await GetTags(request.PartialTagName, request.MaxReturn, context.CancellationToken);
//
//         using var enumerator = tags.GetEnumerator();
//
//         while (enumerator.MoveNext() && !context.CancellationToken.IsCancellationRequested)
//         {
//             var tagName = enumerator.Current;
//             await responseStream.WriteAsync(new FoundTagReply { TagName = tagName }, context.CancellationToken);
//         }
//     }
//
//     /// <summary>
//     ///     Partially matching tags using Aho-corasick search algorithm.
//     ///     Even the simplest matches are streamed (for example one letter match only).
//     ///     Additional filtering/ordering is needed on a client side.
//     /// </summary>
//     public override async Task MatchTags(
//         MatchTagsRequest request,
//         IServerStreamWriter<MatchedTagReply> responseStream,
//         ServerCallContext context)
//     {
//         var dict = request.PartialTagName.GetAllSubstrings().Distinct();
//         var ahoCorasick = new AhoCorasick(dict);
//
//         var tagNames = await GetAllTags(context.CancellationToken);
//
//         foreach (var tagName in tagNames)
//         {
//             var matchedParts = ahoCorasick
//                 .Search(tagName) // todo: safeguard for very long tagNames would be nice
//                 .ExcludeOverlaying(tagName)
//                 .Select(match => new MatchedPart { StartIndex = match.Index, Length = match.Word.Length })
//                 .OrderByDescending(match => match.Length)
//                 .ToList();
//
//             if (matchedParts.Count == 0) continue;
//
//             var matchTagsReply = new MatchedTagReply
//             {
//                 MatchedTagName = tagName,
//                 Score = matchedParts[0].Length * 10 - matchedParts[0].StartIndex,
//                 MatchedParts = { matchedParts }
//             };
//
//             await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
//         }
//     }
//
//     private static async Task<IEnumerable<string>> GetTags(string partialTagName, int requestMaxReturn, CancellationToken ct)
//     {
//         await using var db = new TagContext();
//
//         var query = await db.Tags
//             .AsNoTracking()
//             .Select(tag => tag.Name)
//             .Where(tagName => tagName.StartsWith(partialTagName))
//             .OrderBy(static tagName => tagName)
//             .Take(requestMaxReturn)
//             .ToListAsync(cancellationToken: ct);
//
//         return query;
//     }
//
//     // todo: adjust this method for batch processing for case when we have extremely large number of tags (what is extremely large?)
//     private static async Task<IEnumerable<string>> GetAllTags(CancellationToken ct = default)
//     {
//         await using var db = new TagContext();
//
//         var query = await db.Tags
//             .AsNoTracking()
//             .Select(tag => tag.Name)
//             .ToListAsync(ct);
//
//         return query;
//     }
// }
