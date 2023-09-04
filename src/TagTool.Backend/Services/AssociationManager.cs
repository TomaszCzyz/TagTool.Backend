using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TreeCollections;

namespace TagTool.Backend.Services;

public class RootTagSynonymsGroup : TagSynonymsGroup
{
}

/// <summary>
///     We assume that provided tag always exists.
/// </summary>
public interface IAssociationManager
{
    Task<OneOf<None, ErrorResponse>> AddSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);

    // Task<OneOf<None, ErrorResponse>> RemoveSynonym(TagBase tag, string groupName, CancellationToken cancellationToken);
    Task<OneOf<None, ErrorResponse>> AddChild(TagBase childTag, TagBase parentTag, CancellationToken cancellationToken);
    Task<OneOf<None, ErrorResponse>> RemoveChild(TagBase child, TagBase parent, CancellationToken cancellationToken);
}

public class AssociationManager : IAssociationManager
{
    private const string DefaultGroupSuffix = "TempGroup";

    private readonly TagToolDbContext _dbContext;
    public MutableEntityTreeNode<int, TagSynonymsGroup> Root { get; }

    public AssociationManager(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;

        var rootNode = new RootTagSynonymsGroup { Id = -1, Name = "", Synonyms = Array.Empty<TagBase>() };
        Root = new MutableEntityTreeNode<int, TagSynonymsGroup>(c => c.Id, rootNode);

        var tagsHierarchies = _dbContext.TagsHierarchy
            .Include(tagsHierarchy => tagsHierarchy.ParentGroup)
            .Include(tagsHierarchy => tagsHierarchy.ChildGroups);

        foreach (var tagsHierarchy in tagsHierarchies)
        {
            var treeNode = Root.FirstOrDefault(node => node.Item.Id == tagsHierarchy.ParentGroup.Id) ?? Root.AddChild(tagsHierarchy.ParentGroup);

            foreach (var tagBase in tagsHierarchy.ChildGroups)
            {
                treeNode.AddChild(tagBase);
            }
        }
    }

    private async Task<TagSynonymsGroup> CreateDefaultGroup(TagBase tag, CancellationToken cancellationToken)
    {
        var tagSynonymsGroup = new TagSynonymsGroup { Name = $"{tag.FormattedName}_{DefaultGroupSuffix}", Synonyms = new List<TagBase> { tag } };
        var entry = await _dbContext.TagSynonymsGroups.AddAsync(tagSynonymsGroup, cancellationToken);
        _ = _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity;
    }

    public async Task<OneOf<None, ErrorResponse>> AddSynonym(TagBase tag, string groupName, CancellationToken cancellationToken)
    {
        if (groupName.EndsWith(DefaultGroupSuffix, StringComparison.CurrentCulture))
        {
            return new ErrorResponse(
                $"Cannot manually add tag to group with name ending with '{DefaultGroupSuffix}', as it is reserved for internal use.");
        }

        var tagBase = await _dbContext.Tags.FirstAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);
        var groupWithRequestedTag = await _dbContext.TagSynonymsGroups
            .FirstOrDefaultAsync(group => group.Synonyms.Contains(tagBase), cancellationToken);

        if (groupWithRequestedTag is null)
        {
            return await AddSynonymSimple(tagBase, groupName, cancellationToken);
        }

        if (groupWithRequestedTag.Name == groupName)
        {
            return new ErrorResponse($"The tag {tagBase} is already in a group {groupName}.");
        }

        if (groupWithRequestedTag.Name != groupName && !IsGroupAutomaticallyCreated(groupWithRequestedTag))
        {
            return new ErrorResponse($"The tag {tagBase} is already in different synonyms group {groupWithRequestedTag}");
        }

        // We cope with a group created automatically for child-parent relation for group-less tag
        // i.e. this group can merged into other groups with the same parent tag.
        // We have to remember to inherit parent.
        var (synonymsGroup, _) = await EnsureGroupExists(groupName, cancellationToken);
        var (canBeMerged, hierarchy) = await CanBeMerged(groupWithRequestedTag, synonymsGroup, cancellationToken);

        if (!canBeMerged)
        {
            _dbContext.TagSynonymsGroups.Remove(synonymsGroup);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            return new ErrorResponse(
                $"Tag {tag} already belongs to the group {groupWithRequestedTag}, which cannot be merge into group {synonymsGroup}");
        }

        // merge [group_Temp] into requested group
        _dbContext.TagSynonymsGroups.Remove(groupWithRequestedTag);
        synonymsGroup.Synonyms.Add(tagBase);

        // preserve hierarchy, if exists.
        if (hierarchy?.ChildGroups.Contains(synonymsGroup) == false)
        {
            hierarchy.ChildGroups.Add(synonymsGroup);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return new None();
    }

    private static bool IsGroupAutomaticallyCreated(TagSynonymsGroup existingGroup)
        => existingGroup.Name.EndsWith(DefaultGroupSuffix, StringComparison.CurrentCulture);

    /// <summary>
    ///     Checks if we can merge two groups.
    ///     We can if:
    ///     - both groups have the same parent
    ///     - one group has no parent (it will inherit parent of other group)
    /// </summary>
    /// <param name="group1"></param>
    /// <param name="group2"></param>
    /// <param name="cancellationToken"></param>
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

    private async Task<OneOf<None, ErrorResponse>> AddSynonymSimple(TagBase tagBase, string groupName, CancellationToken cancellationToken)
    {
        var (synonymsGroup, justCreated) = await EnsureGroupExists(groupName, cancellationToken);

        if (!justCreated && synonymsGroup.Synonyms.Contains(tagBase))
        {
            return new ErrorResponse($"The tag {tagBase} is already in synonyms group {synonymsGroup}");
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

        if (synonymsGroup is not null) return (synonymsGroup, false);

        synonymsGroup = new TagSynonymsGroup { Name = groupName, Synonyms = new List<TagBase>() };

        // _logger.LogInformation("Creating new synonym group with the name {SynonymGroupName}", groupName);
        var entry = await _dbContext.TagSynonymsGroups.AddAsync(synonymsGroup, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return (entry.Entity, true);
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

    public Task<OneOf<None, ErrorResponse>> RemoveChild(TagBase child, TagBase parent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
