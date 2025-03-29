using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.DbContexts;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Queries;

public class SearchTagsWildcardRequest : IStreamRequest<(TagBase, IEnumerable<TextSlice>)>
{
    public required string Value { get; init; }

    public int? ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsWildcard : IStreamRequestHandler<SearchTagsWildcardRequest, (TagBase, IEnumerable<TextSlice>)>
{
    private readonly ITagToolDbContext _dbContext;

    public SearchTagsWildcard(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<(TagBase, IEnumerable<TextSlice>)> Handle(
        SearchTagsWildcardRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.Value != "*")
        {
            throw new NotImplementedException();
        }

        var counter = 0;
        await foreach (var tag in _dbContext.Tags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit)
            {
                break;
            }

            counter++;

            yield return (tag, [new TextSlice(0, tag.Text.Length)]);
        }
    }
}
