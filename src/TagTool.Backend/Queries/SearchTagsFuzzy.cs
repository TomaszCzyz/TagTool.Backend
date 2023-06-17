using System.Runtime.CompilerServices;
using Ganss.Text;
using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class SearchTagsFuzzyRequest : IStreamRequest<(TagBase, IEnumerable<MatchedPart>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsFuzzy : IStreamRequestHandler<SearchTagsFuzzyRequest, (TagBase, IEnumerable<MatchedPart>)>
{
    private readonly TagToolDbContext _dbContext;

    public SearchTagsFuzzy(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async IAsyncEnumerable<(TagBase, IEnumerable<MatchedPart>)> Handle(
        SearchTagsFuzzyRequest request,
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

            yield return (tag, matchedParts);
        }
    }
}
