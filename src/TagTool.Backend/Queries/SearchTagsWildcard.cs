using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class SearchTagsWildcardRequest : IStreamRequest<(string, IEnumerable<MatchedPart>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsWildcard : IStreamRequestHandler<SearchTagsWildcardRequest, (string, IEnumerable<MatchedPart>)>
{
    private readonly TagToolDbContext _dbContext;

    public SearchTagsWildcard(TagToolDbContext dbContext) => _dbContext = dbContext;

    public async IAsyncEnumerable<(string, IEnumerable<MatchedPart>)> Handle(
        SearchTagsWildcardRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.Value != "*") throw new NotImplementedException();

        var counter = 0;
        await foreach (var tag in _dbContext.NormalTags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit) break;
            counter++;

            yield return (tag.Name, new[] { new MatchedPart(0, tag.Name.Length) });
        }
    }
}
