using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Queries;

public class GetAllTagsAssociationsQuery : IQuery<(TagBase[] Synonyms, TagBase[] HigherTags)>
{
    public required TagBase TagBase { get; init; }
}

public class GetAllTagsAssociations : IQueryHandler<GetAllTagsAssociationsQuery, (TagBase[], TagBase[])>
{
    private readonly TagToolDbContext _dbContext;

    public GetAllTagsAssociations(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(TagBase[], TagBase[])> Handle(GetAllTagsAssociationsQuery request, CancellationToken cancellationToken)
    {
        if (request.TagBase is null) throw new NotImplementedException();

        var tagBase = _dbContext.Tags.First(@base => @base.FormattedName == request.TagBase.FormattedName);

        var tagAssociations = await _dbContext.Associations
            .Include(associations => associations.Tag)
            .Include(associations => associations.Descriptions)
            .ThenInclude(description => description.Tag)
            .FirstAsync(associations => associations.Tag == tagBase, cancellationToken);

        var tagSynonyms = tagAssociations.Descriptions.Where(d => d.AssociationType == AssociationType.Synonyms).Select(d => d.Tag);
        var higherTags = await FindHigherTags(tagAssociations, cancellationToken);

        return (tagSynonyms.ToArray(), higherTags.ToArray());
    }

    private async Task<IEnumerable<TagBase>> FindHigherTags(TagAssociations tagAssociations, CancellationToken cancellationToken)
    {
        var results = new List<TagBase>();

        foreach (var description in tagAssociations.Descriptions.Where(d => d.AssociationType == AssociationType.IsSubtype))
        {
            results.Add(description.Tag);

            var innerTagAssociations = await _dbContext.Associations
                .Include(associations => associations.Tag)
                .Include(associations => associations.Descriptions)
                .ThenInclude(d => d.Tag)
                .FirstOrDefaultAsync(associations => associations.Tag == description.Tag, cancellationToken);

            if (innerTagAssociations is null) continue;

            results.AddRange(await FindHigherTags(innerTagAssociations, cancellationToken));
        }

        return results;
    }
}
