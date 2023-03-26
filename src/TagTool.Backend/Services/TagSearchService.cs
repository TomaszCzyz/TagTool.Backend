﻿using Ganss.Text;
using Grpc.Core;
using TagTool.Backend.Extensions;
using TagTool.Backend.Repositories;

namespace TagTool.Backend.Services;

public class TagSearchService : Backend.TagSearchService.TagSearchServiceBase
{
    private readonly ITagsRepo _tagsRepo;

    public TagSearchService(ITagsRepo tagsRepo)
    {
        _tagsRepo = tagsRepo;
    }

    public override Task<GetAllReply> GetAll(Empty request, ServerCallContext context)
    {
        var tagNames = _tagsRepo.GetAllTagNames().ToArray();

        var getAllReply = new GetAllReply { TagName = { tagNames } };

        return Task.FromResult(getAllReply);
    }

    public override async Task FindTags(
        FindTagsRequest request,
        IServerStreamWriter<FoundTagReply> responseStream,
        ServerCallContext context)
    {
        var tags = GetTags(request.PartialTagName, request.MaxReturn);

        using var enumerator = tags.GetEnumerator();

        while (enumerator.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var tagName = enumerator.Current;
            await responseStream.WriteAsync(new FoundTagReply { TagNames = tagName }, context.CancellationToken);
        }
    }

    /// <summary>
    ///     Partially matching tags using Aho-corasick search algorithm.
    ///     Even the simplest matches are streamed (for example one letter match only).
    ///     Additional filtering/ordering is needed on a client side.
    /// </summary>
    public override async Task MatchTags(
        MatchTagsRequest request,
        IServerStreamWriter<MatchedTagReply> responseStream,
        ServerCallContext context)
    {
        var dict = request.PartialTagName.Substrings().Distinct();
        var ahoCorasick = new AhoCorasick(dict);

        var tagNames = _tagsRepo.GetAllTagNames();

        foreach (var tagName in tagNames)
        {
            var matchedParts = ahoCorasick
                .Search(tagName) // todo: safeguard for very long tagNames would be nice
                .ExcludeOverlaying(tagName)
                .Select(match => new MatchedPart { StartIndex = match.Index, Length = match.Word.Length })
                .OrderByDescending(match => match.Length)
                .ToList();

            if (matchedParts.Count == 0) continue;

            var matchTagsReply = new MatchedTagReply
            {
                MatchedTagName = tagName,
                Score = matchedParts[0].Length * 10 - matchedParts[0].StartIndex,
                MatchedParts = { matchedParts }
            };

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
            // await Task.Delay(Random.Shared.Next(50, 250));
        }
    }

    private IEnumerable<string> GetTags(string partialTagName, int requestMaxReturn)
        => _tagsRepo
            .GetAllTagNames()
            .Where(tagName => tagName.StartsWith(partialTagName, StringComparison.CurrentCulture))
            .OrderBy(static tagName => tagName)
            .Take(requestMaxReturn);

    // private static async Task<IEnumerable<string>> GetTags(string partialTagName, int requestMaxReturn, CancellationToken ct)
    // {
    //     await using var db = new TagContext();
    //
    //     var query = await db.Tags
    //         .AsNoTracking()
    //         .Select(tag => tag.Name)
    //         .Where(tagName => tagName.StartsWith(partialTagName))
    //         .OrderBy(static tagName => tagName)
    //         .Take(requestMaxReturn)
    //         .ToListAsync(cancellationToken: ct);
    //
    //     return query;
    // }
    //
    // // todo: adjust this method for batch processing for case when we have extremely large number of tags (what is extremely large?)
    // private static async Task<IEnumerable<string>> GetAllTags(CancellationToken ct = default)
    // {
    //     await using var db = new TagContext();
    //
    //     var query = await db.Tags
    //         .AsNoTracking()
    //         .Select(tag => tag.Name)
    //         .ToListAsync(ct);
    //
    //     return query;
    // }
}
