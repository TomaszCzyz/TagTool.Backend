using System.Runtime.CompilerServices;
using Ganss.Text;
using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class SearchTagsPartialRequest : IStreamRequest<(string, IEnumerable<MatchedPart>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsPartial : IStreamRequestHandler<SearchTagsPartialRequest, (string, IEnumerable<MatchedPart>)>
{
    private readonly TagToolDbContext _dbContext;

    public SearchTagsPartial(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async IAsyncEnumerable<(string, IEnumerable<MatchedPart>)> Handle(
        SearchTagsPartialRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var ahoCorasick = new AhoCorasick(request.Value.Substrings().Distinct());

        var counter = 0;
        await foreach (var tag in _dbContext.NormalTags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit) break;
            counter++;

            var tagName = tag.Name;

            var matchedParts = ahoCorasick
                .Search(tagName) // todo: safeguard for very long tagNames would be nice
                .ExcludeOverlaying(tagName)
                .Select(match => new MatchedPart(match.Index, match.Word.Length))
                .OrderByDescending(match => match.Length)
                .ToArray();

            if (matchedParts.Length == 0) continue;

            yield return (tagName, matchedParts);
        }
    }
}
