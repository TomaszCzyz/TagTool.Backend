using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;
public class SearchTagsStartsWithRequest : IStreamRequest<(string, IEnumerable<MatchedPart>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsStartsWith : IStreamRequestHandler<SearchTagsStartsWithRequest, (string, IEnumerable<MatchedPart>)>
{
    private readonly TagToolDbContext _dbContext;

    public SearchTagsStartsWith(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async IAsyncEnumerable<(string, IEnumerable<MatchedPart>)> Handle(
        SearchTagsStartsWithRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = _dbContext.Tags
            .Where(tag => tag.Name.StartsWith(request.Value))
            .Select(tag => tag.Name)
            .Take(request.ResultsLimit);

        var counter = 0;
        await foreach (var tagName in queryable.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit) break;
            counter++;

            var matchedPart = new MatchedPart(0, tagName.IndexOf(request.Value.Last()));
            yield return (tagName, new[] { matchedPart });
        }
    }
}
