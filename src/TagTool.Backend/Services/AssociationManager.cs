using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TreeCollections;

namespace TagTool.Backend.Services;

public class RootTagBase : TagBase
{
}

public interface IAssociationManager
{
    Task<OneOf<None, ErrorResponse>> UpsertSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);
    Task<OneOf<None, ErrorResponse>> RemoveSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);
    Task<OneOf<None, ErrorResponse>> AddChildTag(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken);
    Task<OneOf<None, ErrorResponse>> RemoveChildTag(TagBase child, TagBase parent, CancellationToken cancellationToken);
}

public class AssociationManager : IAssociationManager
{
    private readonly ILogger<AssociationManager> _logger;
    private readonly TagToolDbContext _dbContext;
    public MutableEntityTreeNode<int, TagBase> Root { get; }

    public AssociationManager(ILogger<AssociationManager> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;

        Root = new MutableEntityTreeNode<int, TagBase>(c => c.Id, new RootTagBase { Id = -1 });

        var tagsHierarchies = _dbContext.TagsHierarchy
            .Include(tagsHierarchy => tagsHierarchy.BaseTag)
            .Include(tagsHierarchy => tagsHierarchy.ChildTags);

        foreach (var tagsHierarchy in tagsHierarchies)
        {
            var treeNode = Root.FirstOrDefault(node => node.Item.Id == tagsHierarchy.BaseTag.Id) ?? Root.AddChild(tagsHierarchy.BaseTag);

            foreach (var tagBase in tagsHierarchy.ChildTags)
            {
                treeNode.AddChild(tagBase);
            }
        }
    }

    public async Task<OneOf<None, ErrorResponse>> UpsertSynonym(TagBase tag, string groupName, CancellationToken cancellationToken)
    {
        var synonymsGroup = await EnsureGroupExists(groupName, cancellationToken);
        var tagBase = await EnsureTagExists(tag, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        var group = await _dbContext.TagSynonymsGroup.FirstOrDefaultAsync(group => group.TagsSynonyms.Contains(tagBase), cancellationToken);

        if (group is not null)
        {
            var message = group.Id != synonymsGroup.Id
                ? $"The tag {tagBase} is already in different synonyms group {group}"
                : $"The tag {tagBase} is already in synonyms group {group}";

            return new ErrorResponse(message);
        }

        if (synonymsGroup.TagsSynonyms.Count != 0)
        {
            var anySynonym = synonymsGroup.TagsSynonyms.First();
            var node = Root.First(node => node.Item == anySynonym);
            if (node.Parent != Root) // existing synonym has base tag
            {
                var tagsHierarchy = _dbContext.TagsHierarchy.First(hierarchy => hierarchy.BaseTag == node.Parent.Item);
                tagsHierarchy.ChildTags.Add(tagBase);
            }
        }

        synonymsGroup.TagsSynonyms.Add(tagBase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    private async Task<TagSynonymsGroup> EnsureGroupExists(string groupName, CancellationToken cancellationToken)
    {
        var synonymsGroup = await _dbContext.TagSynonymsGroup
            .Include(group => group.TagsSynonyms)
            .FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);

        if (synonymsGroup is not null) return synonymsGroup;

        synonymsGroup = new TagSynonymsGroup { Name = groupName, TagsSynonyms = new List<TagBase>() };

        _logger.LogInformation("Creating new synonym group with the name {SynonymGroupName}", groupName);
        var entry = await _dbContext.TagSynonymsGroup.AddAsync(synonymsGroup, cancellationToken);

        return entry.Entity;
    }

    private async Task<TagBase> EnsureTagExists(TagBase tagBase, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == tagBase.FormattedName, cancellationToken);

        if (tag is not null) return tag;

        _logger.LogInformation("Creating tag {@TagBase} before upserting new tag association", tagBase);
        var entry = await _dbContext.Tags.AddAsync(tagBase, cancellationToken);

        return entry.Entity;
    }

    public Task<OneOf<None, ErrorResponse>> RemoveSynonym(TagBase tag, string groupName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <para>
    ///     Adds sub-sup relation between tags.
    ///     A tag cannot be a subtype of another tag when:
    ///     1. the tag already has a parent tag,
    ///     2. any of tag's synonyms has a parent tag,
    ///     3. the tag is one of the ancestors of the parent tag,
    ///     4. any of tag's synonyms is an ancestor of the parent tag,
    ///     5. any of tag's synonyms is in relation with ancestor of parent.
    /// </para>
    /// <para>1-5 => tag and its synonyms can ONLY have relation with given parent.</para>
    /// </summary>
    /// <param name="childTag"></param>
    /// <param name="parentTag">The base tag, e.g. Animal can be parent of Cat</param>
    /// <param name="cancellationToken"></param>
    public async Task<OneOf<None, ErrorResponse>> AddChildTag(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken)
    {
        var childNode = Root.FirstOrDefault(node => node.Id == childTag.Id);
        if (childNode is not null && childNode.Parent != Root && childNode.Parent.Item != parentTag)
        {
            return new ErrorResponse($"The child {childTag} has a different parent already.");
        }

        var synonymsGroup = await _dbContext.TagSynonymsGroup
            .Include(tagSynonymsGroup => tagSynonymsGroup.TagsSynonyms)
            .FirstOrDefaultAsync(group => group.TagsSynonyms.Contains(childTag), cancellationToken);

        var tagSynonymsToCheck = synonymsGroup?.TagsSynonyms ?? new[] { childTag };

        if (Root.Any(node => tagSynonymsToCheck.Contains(node.Item) && node.Parent.Item != parentTag))
        {
            return new ErrorResponse("The tag and its synonyms can ONLY have relation with given parent");
        }

        // updating
        foreach (var tagBase in tagSynonymsToCheck)
        {
            // _dbContext.TagsHierarchy.FirstOrDefault()
            var tagsHierarchy = await _dbContext.TagsHierarchy.AddAsync(new TagsHierarchy{BaseTag = parentTag, ChildTags = });
            if (tagsHierarchy is not null)
            {
                tagsHierarchy.BaseTag
            }
        }

        throw new NotImplementedException();
    }

    public Task<OneOf<None, ErrorResponse>> RemoveChildTag(TagBase child, TagBase parent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
