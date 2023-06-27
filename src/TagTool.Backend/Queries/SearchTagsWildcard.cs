using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class SearchTagsWildcardRequest : IStreamRequest<(TagBase, IEnumerable<TextSlice>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsWildcard : IStreamRequestHandler<SearchTagsWildcardRequest, (TagBase, IEnumerable<TextSlice>)>
{
    private readonly TagToolDbContext _dbContext;

    public SearchTagsWildcard(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<(TagBase, IEnumerable<TextSlice>)> Handle(
        SearchTagsWildcardRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.Value != "*") throw new NotImplementedException();

        var counter = 0;
        await foreach (var tag in _dbContext.Tags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit) break;
            counter++;

            yield return (tag, new[] { new TextSlice(0, tag.FormattedName.Length) });
        }
    }
}
