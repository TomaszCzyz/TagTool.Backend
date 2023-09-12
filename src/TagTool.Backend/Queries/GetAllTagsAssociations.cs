using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class GetAllTagsAssociationsQuery : IStreamRequest<ITagsRelationsManager.GroupDescription>
{
    public required TagBase? TagBase { get; init; }
}

[UsedImplicitly]
public class GetAllTagsAssociations : IStreamRequestHandler<GetAllTagsAssociationsQuery, ITagsRelationsManager.GroupDescription>
{
    private readonly ITagsRelationsManager _tagsRelationsManager;
    private readonly TagToolDbContext _dbContext;

    public GetAllTagsAssociations(ITagsRelationsManager tagsRelationsManager, TagToolDbContext dbContext)
    {
        _tagsRelationsManager = tagsRelationsManager;
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<ITagsRelationsManager.GroupDescription> Handle(
        GetAllTagsAssociationsQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tag = request.TagBase switch
        {
            not null => await GetOrCreateTag(request.TagBase, cancellationToken),
            _ => null
        };

        var allRelations = _tagsRelationsManager.GetRelations(tag, cancellationToken);

        await foreach (var groupDescription in allRelations)
        {
            yield return groupDescription;
        }
    }

    private async Task<TagBase> GetOrCreateTag(TagBase tag, CancellationToken cancellationToken)
    {
        var tagBase = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        if (tagBase is not null)
        {
            return tagBase;
        }

        var entry = await _dbContext.Tags.AddAsync(tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity;
    }
}
