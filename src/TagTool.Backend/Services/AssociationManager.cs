using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TreeCollections;

namespace TagTool.Backend.Services;

/// <summary>
///     The interface for managing relation between tags, i.e. tag synonyms and tag child-parent hierarchy.
///     All modification of such relations should be perform via the interface.
///     Collection <see cref="TagTool.Backend.DbContext.ITagToolDbContext.TagSynonymsGroups" /> and
///     <see cref="TagTool.Backend.DbContext.ITagToolDbContext.TagsHierarchy" />
///     should not be accessed only from classes implementing this interface.
/// </summary>
/// <remarks>We assume that provided tag always exists. Providing non-existing tag will cause exception</remarks>
public interface ITagsRelationsManager
{
    Task<OneOf<None, ErrorResponse>> AddSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);

    Task<OneOf<None, ErrorResponse>> RemoveSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);

    Task<OneOf<None, ErrorResponse>> AddChild(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken);

    Task<OneOf<None, ErrorResponse>> RemoveChild(TagBase child, TagBase parent, CancellationToken cancellationToken);

    IAsyncEnumerable<GroupDescription> GetRelations(TagBase? tag, CancellationToken cancellationToken);

    public record GroupDescription(string GroupName, ICollection<TagBase> GroupTags, IList<string> GroupAncestors);
}

public class TagsRelationsManager : ITagsRelationsManager
{
    private const string DefaultGroupSuffix = "TempGroup";

    private readonly ITagToolDbContext _dbContext;

    public TagsRelationsManager(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<None, ErrorResponse>> AddSynonym(TagBase tag, string groupName, CancellationToken cancellationToken)
    {
        if (groupName.EndsWith(DefaultGroupSuffix, StringComparison.CurrentCulture))
        {
            return new ErrorResponse(
                $"Cannot manually add tag to group with name ending with '{DefaultGroupSuffix}', as it is reserved for internal use.");
        }

        var groupWithRequestedTag = await _dbContext.TagSynonymsGroups
            .Include(tagSynonymsGroup => tagSynonymsGroup.Synonyms)
            .FirstOrDefaultAsync(group => group.Synonyms.Contains(tag), cancellationToken);

        if (groupWithRequestedTag is null)
        {
            // tag is not in any group
            return await AddSynonymSimple(tag, groupName, cancellationToken);
        }

        if (groupWithRequestedTag.Name == groupName)
        {
            return new ErrorResponse($"The tag {tag} is already in a requested group {groupWithRequestedTag}.");
        }

        if (groupWithRequestedTag.Name != groupName && !IsGroupAutomaticallyCreated(groupWithRequestedTag))
        {
            return new ErrorResponse($"The tag {tag} is already in different synonyms group {groupWithRequestedTag}");
        }

        // At this point, we cope with a group created automatically for child-parent relation for group-less tag
        // i.e. this group can merge into other groups with the same parent tag.
        // We have to remember to inherit parent.
        var (synonymsGroup, _) = await EnsureGroupExists(groupName, cancellationToken);
        var (canBeMerged, hierarchy) = await CanBeMerged(groupWithRequestedTag, synonymsGroup, cancellationToken);

        if (!canBeMerged)
        {
            // Reverse creating new group
            _dbContext.TagSynonymsGroups.Remove(synonymsGroup);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            return new ErrorResponse(
                $"Tag {tag} already belongs to the group {groupWithRequestedTag}, which cannot be merge into group {synonymsGroup}");
        }

        var mergedGroup = new TagSynonymsGroup
        {
            Name = synonymsGroup.Name, Synonyms = synonymsGroup.Synonyms.Concat(groupWithRequestedTag.Synonyms).ToList()
        };

        // Replace two group with new one, containing tags from both.
        hierarchy?.ChildGroups.Remove(synonymsGroup);
        hierarchy?.ChildGroups.Remove(groupWithRequestedTag);

        _dbContext.TagSynonymsGroups.Remove(synonymsGroup);
        _dbContext.TagSynonymsGroups.Remove(groupWithRequestedTag);

        await _dbContext.TagSynonymsGroups.AddAsync(mergedGroup, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        hierarchy?.ChildGroups.Add(mergedGroup);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    public async Task<OneOf<None, ErrorResponse>> RemoveSynonym(TagBase tag, string groupName, CancellationToken cancellationToken)
    {
        var existingGroup = await _dbContext.TagSynonymsGroups
            .Include(group => group.Synonyms)
            .FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);

        if (existingGroup is null)
        {
            return new ErrorResponse($"The synonyms group with name {groupName} does not exists");
        }

        if (!existingGroup.Synonyms.Contains(tag))
        {
            return new ErrorResponse($"Synonyms group {existingGroup.Name} does not contain tag {tag}");
        }

        existingGroup.Synonyms.Remove(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    public async Task<OneOf<None, ErrorResponse>> AddChild(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken)
    {
        var (childTagBase, parentTagBase) = await GetTags(childTag, parentTag, cancellationToken);
        var (childTagGroup, parentTagGroup) = await GetGroups(childTagBase, parentTagBase, cancellationToken);

        TagsHierarchy? newLink;
        switch (childTagGroup, parentTagGroup)
        {
            case (null, null):
                childTagGroup = await CreateDefaultGroup(childTagBase, cancellationToken);
                parentTagGroup = await CreateDefaultGroup(parentTagBase, cancellationToken);

                // There were no groups, so there was no child-parent relation between groups.
                // We can add link without checking existing hierarchies.
                newLink = new TagsHierarchy { ParentGroup = parentTagGroup, ChildGroups = new List<TagSynonymsGroup> { childTagGroup } };

                _dbContext.TagsHierarchy.Add(newLink);
                break;
            case (not null, null):
                parentTagGroup = await CreateDefaultGroup(parentTagBase, cancellationToken);

                // When we add a child group to a newly created parent group, we need to check if:
                // - child has no other parent
                var existingParentGroup = await _dbContext.TagsHierarchy
                    .FirstOrDefaultAsync(hierarchy => hierarchy.ChildGroups.Contains(childTagGroup), cancellationToken);

                if (existingParentGroup is not null)
                {
                    return new ErrorResponse(
                        $"Tag {childTagBase} is in the group {childTagGroup}, "
                        + $"which has a different parent group {existingParentGroup}. "
                        + "Group of synonyms cannot have two parent groups.");
                }

                newLink = new TagsHierarchy { ParentGroup = parentTagGroup, ChildGroups = new List<TagSynonymsGroup> { childTagGroup } };

                _dbContext.TagsHierarchy.Add(newLink);
                break;
            case (null, not null):
                childTagGroup = await CreateDefaultGroup(childTagBase, cancellationToken);

                // When we add a newly created child group to existing parent group, we do not need to check anything
                // as we know that the group has no parent or children.

                // UPDATE
                var tagsHierarchy = await _dbContext.TagsHierarchy
                    .Include(hierarchy => hierarchy.ChildGroups)
                    .FirstOrDefaultAsync(hierarchy => hierarchy.ParentGroup == parentTagGroup, cancellationToken);

                if (tagsHierarchy is not null)
                {
                    tagsHierarchy.ChildGroups.Add(childTagGroup);
                }
                else
                {
                    newLink = new TagsHierarchy { ParentGroup = parentTagGroup, ChildGroups = new List<TagSynonymsGroup> { childTagGroup } };
                    _dbContext.TagsHierarchy.Add(newLink);
                }

                break;
            case (not null, not null):
                if (childTagGroup == parentTagGroup)
                {
                    return new ErrorResponse($"Tag {childTagBase} and {parentTagBase} already are in the same synonym group");
                }

                // When we add group as a child of another group, we have to check if:
                // - child has no other parent
                var existingParentGroup2 = await _dbContext.TagsHierarchy
                    .FirstOrDefaultAsync(hierarchy => hierarchy.ChildGroups.Contains(childTagGroup), cancellationToken);

                if (existingParentGroup2 is not null)
                {
                    return new ErrorResponse(
                        $"Tag {childTagBase} is in the group {childTagGroup}, "
                        + $"which has a different parent group {existingParentGroup2}. "
                        + "Group of synonyms cannot have two parent groups.");
                }

                // UPDATE
                var tagsHierarchy2 = await _dbContext.TagsHierarchy
                    .Include(hierarchy => hierarchy.ChildGroups)
                    .FirstOrDefaultAsync(hierarchy => hierarchy.ParentGroup == parentTagGroup, cancellationToken);

                if (tagsHierarchy2 is not null)
                {
                    tagsHierarchy2.ChildGroups.Add(childTagGroup);
                }
                else
                {
                    newLink = new TagsHierarchy { ParentGroup = parentTagGroup, ChildGroups = new List<TagSynonymsGroup> { childTagGroup } };
                    _dbContext.TagsHierarchy.Add(newLink);
                }

                break;
        }

        _ = _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    public async Task<OneOf<None, ErrorResponse>> RemoveChild(TagBase child, TagBase parent, CancellationToken cancellationToken)
    {
        var hierarchy = await _dbContext.TagsHierarchy
            .Include(tagsHierarchy => tagsHierarchy.ChildGroups).ThenInclude(tagSynonymsGroup => tagSynonymsGroup.Synonyms)
            .FirstOrDefaultAsync(
                hierarchy => hierarchy.ParentGroup.Synonyms.Contains(parent)
                             && hierarchy.ChildGroups.Any(group => group.Synonyms.Contains(child)),
                cancellationToken);

        if (hierarchy is null)
        {
            return new ErrorResponse($"There is no child-parent relation between {child} and {parent}.");
        }

        var synonymsGroup = hierarchy.ChildGroups.First(group => group.Synonyms.Contains(child));
        hierarchy.ChildGroups.Remove(synonymsGroup);

        // _dbContext.TagsHierarchy.Update()
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    public async IAsyncEnumerable<ITagsRelationsManager.GroupDescription> GetRelations(
        TagBase? tag,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var relationTree = await BuildRelationsTree(cancellationToken);

        if (tag is not null)
        {
            var requestedTagGroup = relationTree.FirstOrDefault(node => node.Item.Synonyms.Contains(tag));
            if (requestedTagGroup is null)
            {
                yield break;
            }

            yield return CreateGroupDescription(requestedTagGroup);
        }
        else
        {
            foreach (var group in relationTree.Where(node => !node.IsRoot))
            {
                yield return CreateGroupDescription(group);
            }
        }
    }

    private static ITagsRelationsManager.GroupDescription CreateGroupDescription(MutableEntityTreeNode<int, TagSynonymsGroup> group)
    {
        var groupName = group.Item.Name;
        var tagsInGroup = group.Item.Synonyms;
        var ancestorsNames = group
            .SelectAncestorsUpward()
            .Where(node => !node.IsRoot)
            .Select(node => node.Item.Name)
            .ToList();

        return new ITagsRelationsManager.GroupDescription(groupName, tagsInGroup, ancestorsNames);
    }

    private async Task<MutableEntityTreeNode<int, TagSynonymsGroup>> BuildRelationsTree(CancellationToken cancellationToken)
    {
        var rootNode = new RootTagSynonymsGroup
        {
            Id = -1,
            Name = "",
            Synonyms = Array.Empty<TagBase>()
        };
        var groupsTree = new MutableEntityTreeNode<int, TagSynonymsGroup>(c => c.Id, rootNode);

        var hierarchies = await _dbContext.TagsHierarchy
            .Include(tagsHierarchy => tagsHierarchy.ParentGroup)
            .Include(tagsHierarchy => tagsHierarchy.ChildGroups)
            .ToArrayAsync(cancellationToken);

        var asAsyncEnumerable = _dbContext.TagSynonymsGroups
            .Include(group => group.Synonyms)
            .AsAsyncEnumerable();

        await foreach (var group in asAsyncEnumerable.WithCancellation(cancellationToken))
        {
            var hierarchy = Array.Find(hierarchies, hierarchy => hierarchy.ParentGroup == group || hierarchy.ChildGroups.Contains(group));
            if (hierarchy is null)
            {
                groupsTree.AddChild(group);
            }
            else
            {
                // Check if parent of this hierarchy is already a part of the tree.
                // If it is, then add group as child. Otherwise, add it to root.
                var parentGroupNode = groupsTree.FirstOrDefault(node => node.Item == hierarchy.ParentGroup);
                if (parentGroupNode is not null)
                {
                    parentGroupNode.AddChild(group);
                }
                else
                {
                    groupsTree.AddChild(group);
                }

                AdjustTree(groupsTree, hierarchies);
            }
        }

        return groupsTree;
    }

    /// <summary>
    ///     Checks if nodes temporarily attached to root can be moved to correct parent.
    /// </summary>
    private static void AdjustTree(MutableEntityTreeNode<int, TagSynonymsGroup> groupsTree, TagsHierarchy[] hierarchies)
    {
        foreach (var groupNode in groupsTree.Children.ToArray())
        {
            // If there is parent of the group present in a tree, then move group to the parent
            if (HasParent(groupNode, hierarchies, out var hierarchy))
            {
                var parentGroup = groupsTree.FirstOrDefault(node => node.Item == hierarchy.ParentGroup);
                if (parentGroup is not null)
                {
                    groupNode.MoveToParent(parentGroup.Id);
                }
            }
        }
    }

    private static bool HasParent(
        MutableEntityTreeNode<int, TagSynonymsGroup> groupNode,
        TagsHierarchy[] hierarchies,
        [NotNullWhen(true)] out TagsHierarchy? tagsHierarchy)
    {
        var elem = Array.Find(hierarchies, hierarchy => hierarchy.ChildGroups.Contains(groupNode.Item));
        if (elem is null)
        {
            tagsHierarchy = null;
            return false;
        }

        tagsHierarchy = elem;
        return true;
    }

    /// <summary>
    ///     Checks if we can merge two groups.
    ///     We can if:
    ///     - both groups have the same parent
    ///     - one group has no parent (it will inherit parent of other group)
    /// </summary>
    private async Task<(bool, TagsHierarchy?)> CanBeMerged(TagSynonymsGroup group1, TagSynonymsGroup group2, CancellationToken cancellationToken)
    {
        var (hierarchy1, hierarchy2) = await GetHierarchies(group1, group2, cancellationToken);

        if (hierarchy1 is null && hierarchy2 is null)
        {
            return (true, null);
        }

        if (hierarchy1 is null && hierarchy2 is not null)
        {
            return (true, hierarchy2);
        }

        if (hierarchy1 is not null && hierarchy2 is null)
        {
            return (true, hierarchy1);
        }

        // are groups siblings?
        if (hierarchy1?.ParentGroup == hierarchy2?.ParentGroup)
        {
            return (true, hierarchy1);
        }

        return (false, null);
    }

    private static bool IsGroupAutomaticallyCreated(TagSynonymsGroup existingGroup)
        => existingGroup.Name.EndsWith(DefaultGroupSuffix, StringComparison.CurrentCulture);

    private async Task<OneOf<None, ErrorResponse>> AddSynonymSimple(TagBase tagBase, string groupName, CancellationToken cancellationToken)
    {
        var (synonymsGroup, justCreated) = await EnsureGroupExists(groupName, cancellationToken);

        if (!justCreated && synonymsGroup.Synonyms.Contains(tagBase))
        {
            return new ErrorResponse($"The tag {tagBase} is already in the synonyms group {synonymsGroup}");
        }

        synonymsGroup.Synonyms.Add(tagBase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }

    private async Task<(TagSynonymsGroup Group, bool JustCreated)> EnsureGroupExists(string groupName, CancellationToken cancellationToken)
    {
        var synonymsGroup = await _dbContext.TagSynonymsGroups
            .Include(group => group.Synonyms)
            .FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);

        if (synonymsGroup is not null)
        {
            return (synonymsGroup, false);
        }

        synonymsGroup = new TagSynonymsGroup { Name = groupName, Synonyms = new List<TagBase>() };

        // _logger.LogInformation("Creating new synonym group with the name {SynonymGroupName}", groupName);
        _ = await _dbContext.TagSynonymsGroups.AddAsync(synonymsGroup, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return (synonymsGroup, true);
    }

    private async Task<TagSynonymsGroup> CreateDefaultGroup(TagBase tag, CancellationToken cancellationToken)
    {
        var tagSynonymsGroup = new TagSynonymsGroup { Name = $"{tag.FormattedName}_{DefaultGroupSuffix}", Synonyms = new List<TagBase> { tag } };

        _ = await _dbContext.TagSynonymsGroups.AddAsync(tagSynonymsGroup, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return tagSynonymsGroup;
    }

    private async Task<(TagsHierarchy? Hierarchy1, TagsHierarchy? Hierarchy2)> GetHierarchies(
        TagSynonymsGroup group1,
        TagSynonymsGroup group2,
        CancellationToken cancellationToken)
    {
        var hierarchies = await _dbContext.TagsHierarchy
            .Where(hierarchy => hierarchy.ChildGroups.Any(group => group == group1 || group == group2))
            .Include(hierarchy => hierarchy.ChildGroups)
            .ToArrayAsync(cancellationToken);

        return hierarchies.Length switch
        {
            2 => hierarchies[0].ChildGroups.Contains(group1) ? (hierarchies[0], hierarchies[1]) : (hierarchies[1], hierarchies[0]),
            1 => hierarchies[0].ChildGroups.Contains(group1) ? (hierarchies[0], null) : (null, hierarchies[0]),
            _ => (null, null)
        };
    }

    private async Task<(TagSynonymsGroup? ChildTagGroup, TagSynonymsGroup? ParentTagGroup)> GetGroups(
        TagBase childTag,
        TagBase parentTag,
        CancellationToken cancellationToken)
    {
        var groups = await _dbContext.TagSynonymsGroups
            .Where(group => group.Synonyms.Any(tagBase => tagBase == childTag || tagBase == parentTag))
            .Include(tagSynonymsGroup => tagSynonymsGroup.Synonyms)
            .ToArrayAsync(cancellationToken);

        return groups.Length switch
        {
            2 => groups[0].Synonyms.Contains(childTag) ? (groups[0], groups[1]) : (groups[1], groups[0]),
            1 => groups[0].Synonyms.Contains(childTag) ? (groups[0], null) : (null, groups[0]),
            _ => (null, null)
        };
    }

    private async Task<(TagBase ChildTag, TagBase ParentTag)> GetTags(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken)
    {
        var tagBases = await _dbContext.Tags
            .Where(tagBase => tagBase.FormattedName == childTag.FormattedName || tagBase.FormattedName == parentTag.FormattedName)
            .ToArrayAsync(cancellationToken);

        return tagBases[0].FormattedName == childTag.FormattedName
            ? (tagBases[0], tagBases[1])
            : (tagBases[1], tagBases[0]);
    }

    private class RootTagSynonymsGroup : TagSynonymsGroup;
}
