using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Commands;

public class RemoveTagSynonymCommand : ICommand<OneOf<string, ErrorResponse>>
{
    public required string GroupName { get; init; }

    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public class RemoveTagSynonym : ICommandHandler<RemoveTagSynonymCommand, OneOf<string, ErrorResponse>>
{
    private readonly TagToolDbContext _dbContext;

    public RemoveTagSynonym(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(RemoveTagSynonymCommand request, CancellationToken cancellationToken)
    {
        var existingGroup = await _dbContext.TagSynonymsGroups
            .Include(group => group.Synonyms)
            .FirstOrDefaultAsync(group => group.Name == request.GroupName, cancellationToken);

        if (existingGroup is null)
        {
            return new ErrorResponse($"The synonyms group with name {request.GroupName} does not exists");
        }

        var result = await RemoveSynonymInner(request.Tag, existingGroup, cancellationToken);

        return result.Match<OneOf<string, ErrorResponse>>(_ => $"successfully removed synonym from group {request.GroupName}", err => err);
    }

    private async Task<OneOf<None, ErrorResponse>> RemoveSynonymInner(
        TagBase tag,
        TagSynonymsGroup existingGroup,
        CancellationToken cancellationToken)
    {
        var tagBase = await _dbContext.Tags.FirstAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        if (!existingGroup.Synonyms.Contains(tag))
        {
            return new ErrorResponse($"Synonyms group {existingGroup.Name} does not contain tag {tagBase}");
        }

        existingGroup.Synonyms.Remove(tagBase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }
}
