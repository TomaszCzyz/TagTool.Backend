using System.Runtime.CompilerServices;
using Ganss.Text;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Queries;

public class SearchTagsFuzzyRequest : IStreamRequest<(TagBase, IEnumerable<TextSlice>)>
{
    public required string Value { get; init; }

    public int ResultsLimit { get; init; } = 20;
}

[UsedImplicitly]
public class SearchTagsFuzzy : IStreamRequestHandler<SearchTagsFuzzyRequest, (TagBase, IEnumerable<TextSlice>)>
{
    private readonly ITagToolDbContext _dbContext;

    public SearchTagsFuzzy(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<(TagBase, IEnumerable<TextSlice>)> Handle(
        SearchTagsFuzzyRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // todo: extract 'empty' search functionality
        if (string.IsNullOrEmpty(request.Value))
        {
            await foreach (var tag in _dbContext.Tags.Take(request.ResultsLimit).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return (tag, new[] { new TextSlice(0, tag.FormattedName.Length - tag.FormattedName.IndexOf(':')) });
            }
        }

        var ahoCorasick = new AhoCorasick(request.Value.Substrings().Distinct());
        var results = new List<(TagBase Tag, TextSlice[] Slices)>();

        var counter = 0;
        await foreach (var tag in _dbContext.Tags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (counter == request.ResultsLimit)
            {
                break;
            }

            var tagName = tag.FormattedName[(tag.FormattedName.IndexOf(':') + 1)..];

            var textSlices = ahoCorasick
                .Search(tagName) // todo: safeguard for very long tagNames would be nice
                .ExcludeOverlaying(tagName)
                .Select(match => new TextSlice(match.Index, match.Word.Length))
                .OrderByDescending(match => match.Length)
                .ToArray();

            if (textSlices.Length == 0)
            {
                continue;
            }

            results.Add((tag, textSlices));
            counter++;
        }

        foreach (var (tag, matches) in results.OrderByDescending(result => result.Slices[0].Length))
        {
            yield return (tag, matches);
        }
    }
}
