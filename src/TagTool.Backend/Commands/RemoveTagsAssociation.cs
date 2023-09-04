// using JetBrains.Annotations;
// using Microsoft.EntityFrameworkCore;
// using OneOf;
// using OneOf.Types;
// using TagTool.Backend.DbContext;
// using TagTool.Backend.Models;
// using TagTool.Backend.Models.Tags;
//
// namespace TagTool.Backend.Commands;
//
// public class RemoveTagsAssociationCommand : ICommand<OneOf<None, ErrorResponse>>
// {
//     public required TagBase FirstTag { get; init; }
//
//     public required TagBase SecondTag { get; init; }
//
//     public required Models.AssociationType AssociationType { get; init; }
// }
//
// [UsedImplicitly]
// public class RemoveTagsAssociation : ICommandHandler<RemoveTagsAssociationCommand, OneOf<None, ErrorResponse>>
// {
//     private readonly TagToolDbContext _dbContext;
//
//     public RemoveTagsAssociation(TagToolDbContext dbContext)
//     {
//         _dbContext = dbContext;
//     }
//
//     public async Task<OneOf<None, ErrorResponse>> Handle(RemoveTagsAssociationCommand request, CancellationToken cancellationToken)
//     {
//         var associations = await _dbContext.Associations
//             .Include(tagAssociations => tagAssociations.Descriptions)
//             .ThenInclude(associationDescription => associationDescription.Tag)
//             .FirstOrDefaultAsync(
//                 associations => associations.Tag.FormattedName == request.FirstTag.FormattedName
//                                 && associations.Descriptions.Any(
//                                     d => d.AssociationType == request.AssociationType && d.Tag.FormattedName == request.SecondTag.FormattedName),
//                 cancellationToken);
//
//         if (associations is null)
//         {
//             return new ErrorResponse(
//                 $"There is no association {request.AssociationType} between tag {request.FirstTag} and tag {request.SecondTag} in db");
//         }
//
//         await RemoveAllSynonyms(associations, cancellationToken);
//
//         if (request.AssociationType == Models.AssociationType.IsSubtype)
//         {
//             var associationDescription = associations.Descriptions.First(
//                 d => d.AssociationType == Models.AssociationType.IsSubtype && d.Tag.FormattedName == request.SecondTag.FormattedName);
//
//             associations.Descriptions.Remove(associationDescription);
//
//             await _dbContext.SaveChangesAsync(cancellationToken);
//         }
//
//         return new None();
//     }
//
//     private async Task RemoveAllSynonyms(TagAssociations tagAssociations, CancellationToken cancellationToken)
//     {
//         var idsOfSynonyms = tagAssociations.Descriptions
//             .Where(description => description.AssociationType == Models.AssociationType.Synonyms)
//             .Select(description => description.TagAssociationsId)
//             .ToList();
//
//         var descriptionContainingSynonymToDelete = _dbContext.Associations
//             .Include(associations => associations.Descriptions)
//             .ThenInclude(description => description.Tag)
//             .Where(associations => idsOfSynonyms.Contains(associations.Id))
//             .Select(associations => associations.Descriptions);
//
//         foreach (var associationDescriptions in descriptionContainingSynonymToDelete)
//         {
//             var description = associationDescriptions.Find(description => description.Tag == tagAssociations.Tag);
//             if (description is not null)
//             {
//                 _dbContext.Remove(description);
//             }
//         }
//
//         tagAssociations.Descriptions.RemoveAll(description => description.AssociationType == Models.AssociationType.Synonyms);
//         await _dbContext.SaveChangesAsync(cancellationToken);
//     }
// }
