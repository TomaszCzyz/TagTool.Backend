using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
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

        switch (request.AssociationType)
        {
            case AssociationType.Synonyms:
                return await UpsertSynonymAssociationToBothTags(firstTag, secondTag, cancellationToken);
            case AssociationType.IsSubtype:
                return await UpsertSubtypeAssociationToSupTag(firstTag, secondTag, cancellationToken);
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

    private async Task<OneOf<string, ErrorResponse>> UpsertSynonymAssociationToBothTags(
        TagBase firstTag,
        TagBase secondTag,
        CancellationToken cancellationToken)
    {
        if (!CanAddSynonymAssociation(firstTag, secondTag, out var errorResponse))
        {
            return errorResponse;
        }

        var (associationsOfFirstTag, associationsOfSecondTag) = await EnsureAssociationsExist(firstTag, secondTag, cancellationToken);

        var allSynonyms = associationsOfFirstTag.Descriptions
            .Concat(associationsOfSecondTag.Descriptions)
            .Where(d => d.AssociationType == AssociationType.Synonyms)
            .Select(d => d.Tag)
            .Append(firstTag)
            .Append(secondTag)
            .Distinct()
            .ToArray();

        var tagAssociationsOfAllSynonyms = _dbContext.Associations
            .Where(associations => allSynonyms.Contains(associations.Tag))
            .Include(tagAssociations => tagAssociations.Descriptions)
            .ThenInclude(associationDescription => associationDescription.Tag)
            .Include(tagAssociations => tagAssociations.Tag);

        foreach (var tagAssociation in tagAssociationsOfAllSynonyms)
        {
            AddMissingDescriptions(allSynonyms, tagAssociation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return "success";
    }

    private static void AddMissingDescriptions(TagBase[] allSynonyms, TagAssociations tagAssociation)
    {
        var allMissingTagSynonyms = allSynonyms
            .Where(tagSynonym => !tagAssociation.Descriptions
                .Select(description => description.Tag)
                .Contains(tagSynonym));

        foreach (var tagSynonym in allMissingTagSynonyms)
        {
            if (tagSynonym == tagAssociation.Tag) continue;

            tagAssociation.Descriptions.Add(
                new AssociationDescription { Tag = tagSynonym, AssociationType = AssociationType.Synonyms, TagAssociationsId = tagAssociation.Id });
        }
    }

    private async Task<(TagAssociations, TagAssociations)> EnsureAssociationsExist(
        TagBase firstTag,
        TagBase secondTag,
        CancellationToken cancellationToken)
    {
        var associationsOfFirstTag = await _dbContext.Associations
            .Include(associations => associations.Descriptions)
            .ThenInclude(associationDescription => associationDescription.Tag)
            .FirstOrDefaultAsync(associations => associations.Tag == firstTag, cancellationToken);

        var associationsOfSecondTag = await _dbContext.Associations
            .Include(associations => associations.Descriptions)
            .ThenInclude(associationDescription => associationDescription.Tag)
            .FirstOrDefaultAsync(associations => associations.Tag == secondTag, cancellationToken);

        if (associationsOfFirstTag is null)
        {
            associationsOfFirstTag = new TagAssociations { Tag = firstTag, Descriptions = new List<AssociationDescription>() };
            await _dbContext.Associations.AddAsync(associationsOfFirstTag, cancellationToken);
        }

        if (associationsOfSecondTag is null)
        {
            associationsOfSecondTag = new TagAssociations { Tag = secondTag, Descriptions = new List<AssociationDescription>() };
            await _dbContext.Associations.AddAsync(associationsOfSecondTag, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (associationsOfFirstTag, associationsOfSecondTag);
    }

    /// <summary>
    ///     There cannot be the following situations:
    ///     1. given association description already exists
    ///     2. tags are in a Sub/Sup type association
    /// </summary>
    private bool CanAddSynonymAssociation(TagBase firstTag, TagBase secondTag, [NotNullWhen(false)] out ErrorResponse? error)
    {
        var associationsOfFirstTagWithSecondTag = AssociationsOfTagContainingCertainTag(firstTag, secondTag);
        var associationsOfSecondTagWithFirstTag = AssociationsOfTagContainingCertainTag(secondTag, firstTag);

        var descriptionWithSecondTag = associationsOfFirstTagWithSecondTag?.Descriptions.First(description => description.Tag == secondTag);
        var descriptionWithFirstTag = associationsOfSecondTagWithFirstTag?.Descriptions.First(description => description.Tag == firstTag);

        foreach (var description in new[] { descriptionWithFirstTag, descriptionWithSecondTag }.Where(d => d is not null))
        {
            switch (description!.AssociationType)
            {
                case AssociationType.Synonyms:
                    error = new ErrorResponse("Given association already exists.");
                    return false;

                case AssociationType.IsSubtype:
                    error = new ErrorResponse("The first tag is a subtype of second tag, so it cannot be its synonym.");
                    return false;
            }
        }

        error = null;
        return true;
    }

    private async Task<OneOf<string, ErrorResponse>> UpsertSubtypeAssociationToSupTag(
        TagBase firstTag,
        TagBase secondTag,
        CancellationToken cancellationToken)
    {
        if (!CanAddSubtypeAssociation(firstTag, secondTag, out var errorResponse))
        {
            return errorResponse;
        }

        var newDescription = new AssociationDescription { Tag = secondTag, AssociationType = AssociationType.IsSubtype };

        var associationsOfFirstTag = await _dbContext.Associations
            .Include(associations => associations.Descriptions)
            .ThenInclude(associationDescription => associationDescription.Tag)
            .FirstOrDefaultAsync(associations => associations.Tag == firstTag, cancellationToken);

        string response;
        if (associationsOfFirstTag is not null)
        {
            associationsOfFirstTag.Descriptions.AddIfNotExists(newDescription);
            response = "New association description was added.";
        }
        else
        {
            var newAssociation = new TagAssociations { Tag = firstTag, Descriptions = new List<AssociationDescription> { newDescription } };
            await _dbContext.Associations.AddAsync(newAssociation, cancellationToken);
            response = "Brand new tags association was added.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <summary>
    ///     There cannot be the following situations:
    ///     1. given association description already exists
    ///     2. the second tag cannot be subtype of first tag (because we want ot add first atg as subtype of the second tag)
    ///     The information of subtype is only stored in child tag, i.e.
    ///     If we have association Cat -> Animal, then we will have only one AssociationDescription: Cat(firstTag): {Animal(SecondTag), Subtype}
    /// </summary>
    private bool CanAddSubtypeAssociation(TagBase firstTag, TagBase secondTag, [NotNullWhen(false)] out ErrorResponse? error)
    {
        var associationsOfFirstTagWithSecondTag = AssociationsOfTagContainingCertainTag(firstTag, secondTag);
        var associationsOfSecondTagWithFirstTag = AssociationsOfTagContainingCertainTag(secondTag, firstTag);

        var descriptionOfFirstTagWithSecondTag = associationsOfFirstTagWithSecondTag?.Descriptions.First(description => description.Tag == secondTag);
        var descriptionOfSecondTagWithFirstTag = associationsOfSecondTagWithFirstTag?.Descriptions.First(description => description.Tag == firstTag);

        var descriptions = new[] { descriptionOfSecondTagWithFirstTag, descriptionOfFirstTagWithSecondTag };

        if (descriptions.Any(description => description?.AssociationType == AssociationType.Synonyms))
        {
            error = new ErrorResponse("The tags are in synonyms association.");
            return false;
        }

        if (descriptionOfSecondTagWithFirstTag?.AssociationType == AssociationType.IsSubtype)
        {
            error = new ErrorResponse("The second tag is a subtype of the first tag, so the first tag cannot be a subtype of the second tag.");
            return false;
        }

        error = null;
        return true;
    }

    private TagAssociations? AssociationsOfTagContainingCertainTag(TagBase firstTag, TagBase secondTag)
        => _dbContext.Associations
            .Include(associations => associations.Descriptions)
            .ThenInclude(associationDescription => associationDescription.Tag)
            .FirstOrDefault(associations
                => associations.Tag == firstTag
                   && associations.Descriptions.Select(description => description.Tag).Contains(secondTag));
}
