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
}
