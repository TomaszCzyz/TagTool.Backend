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
    /// <returns>Tags that should be added to item</returns>
    IEnumerable<TagBase> GetImplicitTags(TaggableItem taggableItem);
}

/// <summary>
///     Returns tags that should be added to <see cref="TaggableItem"/>.
///     Pipeline looks like this:
///     1. Get tags that depend on TaggableItem type.
///     2. Get tags that are associated with already added tags.
/// </summary>
public class ImplicitTagsProvider : IImplicitTagsProvider
{
    private readonly Dictionary<string, TagBase[]> _extensionsToTagsMap
        = new()
        {
            { ".txt", new TagBase[] { new TextTag { Text = "TXT" }, new TextTag { Text = "Text" } } },
            { ".jpg", new TagBase[] { new TextTag { Text = "JPG" }, new TextTag { Text = "Photo" }, new TextTag { Text = "Graphics" } } },
            { ".png", new TagBase[] { new TextTag { Text = "PNG" }, new TextTag { Text = "Graphics" } } },
            { ".pdf", new TagBase[] { new TextTag { Text = "PDF" }, new TextTag { Text = "Document" } } },
            { ".svg", new TagBase[] { new TextTag { Text = "SVG" }, new TextTag { Text = "Graphics" } } },
            { ".docx", new TagBase[] { new TextTag { Text = "DOCX" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" } } },
            { ".doc", new TagBase[] { new TextTag { Text = "DOC" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" } } }
        };

    private readonly TagToolDbContext _dbContext;

    public ImplicitTagsProvider(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;

        EnsureTagsExist();
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

    public IEnumerable<TagBase> GetImplicitTags(TaggableItem taggableItem)
    {
        var itemDependentTags = GetItemDependentTags(taggableItem);
        // var associatedTags = GetAssociatedTags(taggableItem.Tags.Concat(itemDependentTags));
        var associatedTags = Enumerable.Empty<TagBase>();

        return taggableItem.Tags.Concat(itemDependentTags).Concat(associatedTags);
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

    // private IEnumerable<TagBase> GetAssociatedTags(IEnumerable<TagBase> tags)
    //     => tags
    //         .Select(tag => _dbContext.Associations
    //             .Include(associations => associations.Descriptions)
    //             .ThenInclude(tagAssociation => tagAssociation.Tag)
    //             .FirstOrDefault(assoc => assoc.Tag == tag))
    //         .Where(tagsAssociation => tagsAssociation is not null)
    //         .SelectMany(tagsAssociation => tagsAssociation!.Descriptions, (_, association) => association.Tag);
}
