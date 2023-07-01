using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Commands;

public class UpsertTagsAssociationRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required TagBase FirstTag { get; init; }

    public required TagBase SecondTag { get; init; }

    public required AssociationType AssociationType { get; init; }
}

[UsedImplicitly]
public class UpsertTagsAssociation : ICommandHandler<UpsertTagsAssociationRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<UpsertTagsAssociation> _logger;
    private readonly TagToolDbContext _dbContext;

    public UpsertTagsAssociation(ILogger<UpsertTagsAssociation> logger, TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(UpsertTagsAssociationRequest request, CancellationToken cancellationToken)
    {
        var (firstTag, secondTag) = await EnsureTagsExist(request.FirstTag, request.SecondTag, cancellationToken);

        var tagsAssociation = _dbContext.TagsAssociation.FirstOrDefault(assoc => assoc.FirstTag == firstTag && assoc.SecondTag == secondTag);

        if (tagsAssociation is not null)
        {
            tagsAssociation.AssociationType = request.AssociationType;
        }
        else
        {
            var _ = await _dbContext.TagsAssociation.AddAsync(
                new TagsAssociation { FirstTag = firstTag, SecondTag = secondTag, AssociationType = request.AssociationType },
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return "Successfully upserted tag new association.";
    }

    private async Task<(TagBase, TagBase)> EnsureTagsExist(TagBase firstTag, TagBase secondTag, CancellationToken cancellationToken)
    {
        var tag1 = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == firstTag.FormattedName, cancellationToken);
        var tag2 = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == secondTag.FormattedName, cancellationToken);

        if (tag1 is null)
        {
            _logger.LogInformation("Creating tag {@TagBase} before upserting new tag association", firstTag);
            var entityEntry = await _dbContext.Tags.AddAsync(firstTag, cancellationToken);
            tag1 = entityEntry.Entity;
        }

        if (tag2 is null)
        {
            _logger.LogInformation("Creating tag {@TagBase} before upserting new tag association", secondTag);
            var entityEntry = await _dbContext.Tags.AddAsync(secondTag, cancellationToken);
            tag2 = entityEntry.Entity;
        }

        return (tag1, tag2);
    }
}
