using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Services;

public interface IImplicitTagsProvider
{
    /// <summary>
    ///     Calculates implicit tags.
    /// </summary>
    /// <param name="taggableItem">Existing entity with correct id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tags that should be added to item</returns>
    Task<IEnumerable<TagBase>> GetImplicitTags(TaggableItem taggableItem, CancellationToken cancellationToken);
}

/// <summary>
///     Returns tags that should be added to <see cref="TaggableItem" />.
///     Pipeline looks like this:
///     1. Get tags that depend on TaggableItem type.
///     2. Get tags that are associated with already added tags.
/// </summary>
public class ImplicitTagsProvider : IImplicitTagsProvider
{
    private readonly Dictionary<string, TagBase[]> _extensionsToTagsMap
        = new()
        {
            { ".txt", [new TextTag { Text = "TXT" }, new TextTag { Text = "Text" }] },
            { ".jpg", [new TextTag { Text = "JPG" }, new TextTag { Text = "Photo" }, new TextTag { Text = "Graphics" }] },
            { ".png", [new TextTag { Text = "PNG" }, new TextTag { Text = "Graphics" }] },
            { ".pdf", [new TextTag { Text = "PDF" }, new TextTag { Text = "Document" }] },
            { ".svg", [new TextTag { Text = "SVG" }, new TextTag { Text = "Graphics" }] },
            { ".docx", [new TextTag { Text = "DOCX" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" }] },
            { ".doc", [new TextTag { Text = "DOC" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" }] }
        };

    private readonly ITagsRelationsManager _tagsRelationsManager;
    private readonly ITagToolDbContext _dbContext;

    public ImplicitTagsProvider(ITagToolDbContext dbContext, ITagsRelationsManager tagsRelationsManager)
    {
        _dbContext = dbContext;
        _tagsRelationsManager = tagsRelationsManager;

        EnsureTagsExist();
    }

    public async Task<IEnumerable<TagBase>> GetImplicitTags(TaggableItem taggableItem, CancellationToken cancellationToken)
    {
        var itemDependentTags = GetItemDependentTags(taggableItem);
        var associatedTags = await GetAssociatedTags(taggableItem.Tags.Concat(itemDependentTags), cancellationToken);

        return taggableItem.Tags.Concat(itemDependentTags).Concat(associatedTags);
    }

    private void EnsureTagsExist()
    {
        var dictTagNames = _extensionsToTagsMap.Values
            .SelectMany(bases => bases)
            .Select(@base => @base.FormattedName)
            .ToArray();

        var existingTags = _dbContext.Tags
            .Where(tagBase => dictTagNames.Contains(tagBase.FormattedName))
            .Select(@base => @base.FormattedName)
            .ToArray();

        var newTags = _extensionsToTagsMap.Values
            .SelectMany(bases => bases)
            .ExceptBy(existingTags, s => s.FormattedName)
            .ToArray();

        _dbContext.Tags.AddRange(newTags);
        _dbContext.SaveChanges();
    }

    private IQueryable<TagBase> GetItemDependentTags(TaggableItem taggableItem)
    {
        var newTags = new List<TagBase> { new ItemTypeTag { Type = taggableItem.GetType() } };

        if (taggableItem is TaggableFile file && _extensionsToTagsMap.TryGetValue(Path.GetExtension(file.Path), out var tags))
        {
            newTags.AddRange(tags);
        }

        return _dbContext.Tags.Where(tagBase => newTags.Select(@base => @base.FormattedName).Contains(tagBase.FormattedName));
    }

    private async Task<IEnumerable<TagBase>> GetAssociatedTags(IEnumerable<TagBase> tags, CancellationToken cancellationToken)
    {
        var tagsToAdd = new List<TagBase>();
        var ancestorGroupNames = new List<string>();
        foreach (var tag in tags)
        {
            var tagRelations = _tagsRelationsManager.GetRelations(tag, cancellationToken);

            await foreach (var groupDescription in tagRelations)
            {
                tagsToAdd.AddRange(groupDescription.GroupTags);
                ancestorGroupNames.AddRange(groupDescription.GroupAncestors);
            }
        }

        var tagFromAncestorGroups = await _dbContext.TagSynonymsGroups
            .Include(group => group.Synonyms)
            .Where(group => ancestorGroupNames.Contains(group.Name))
            .SelectMany(group => group.Synonyms)
            .ToListAsync(cancellationToken);

        return tagsToAdd.Concat(tagFromAncestorGroups);
    }
}
