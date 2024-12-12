﻿using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Queries;

public class SearchTagsStartsWithRequest : IStreamRequest<(TagBase, IEnumerable<TextSlice>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsStartsWith : IStreamRequestHandler<SearchTagsStartsWithRequest, (TagBase, IEnumerable<TextSlice>)>
{
    private readonly ITagToolDbContext _dbContext;

    public SearchTagsStartsWith(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<(TagBase, IEnumerable<TextSlice>)> Handle(
        SearchTagsStartsWithRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // todo: fix this search; I can add SearchName column to Tag table that will contain FormattedName trimmed form tag Type
        var queryable = _dbContext.Tags
            .Where(tag => tag.Text.StartsWith(request.Value))
            .Take(request.ResultsLimit);

        var counter = 0;
        await foreach (var tag in queryable.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit)
            {
                break;
            }

            counter++;

            var matchedPart = new TextSlice(0, tag.Text.IndexOf(request.Value.Last()));
            yield return (tag, [matchedPart]);
        }
    }
}
