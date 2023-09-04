using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

// todo: rename to Add..
public class UpsertTagSynonymRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string GroupName { get; init; }

    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public class UpsertTagSynonym : ICommandHandler<UpsertTagSynonymRequest, OneOf<string, ErrorResponse>>
{
    private readonly TagToolDbContext _dbContext;
    private readonly ILogger<UpsertTagSynonym> _logger;
    private readonly AssociationManager _associationManager;

    public UpsertTagSynonym(ILogger<UpsertTagSynonym> logger, TagToolDbContext dbContext, AssociationManager associationManager)
    {
        _logger = logger;
        _dbContext = dbContext;
        _associationManager = associationManager;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(UpsertTagSynonymRequest request, CancellationToken cancellationToken)
    {
        var addSynonym = await _associationManager.AddSynonym(request.Tag, request.GroupName, cancellationToken);

        return addSynonym.Match(_ => "successfully added synonym", response => response.Message);
    }

    // public async Task<OneOf<string, ErrorResponse>> Handle(UpsertTagSynonymRequest request, CancellationToken cancellationToken)
    // {
    //     var tagBase = await _dbContext.Tags.FirstAsync(t => t.FormattedName == request.Tag.FormattedName, cancellationToken);
    //     var existingGroup = await _dbContext.TagSynonymsGroups.FirstOrDefaultAsync(group => group.Synonyms.Contains(tagBase), cancellationToken);
    //
    //     if (existingGroup is not null && existingGroup.Name != request.GroupName)
    //     {
    //         return new ErrorResponse($"The tag {tagBase} is already in different synonyms group {existingGroup}");
    //     }
    //
    //     var result = await AddSynonymInner(tagBase, request.GroupName, cancellationToken);
    //
    //     return result.Match<OneOf<string, ErrorResponse>>(_ => $"successfully added synonym to group {request.GroupName}", err => err);
    // }
    //
    // private async Task<OneOf<None, ErrorResponse>> AddSynonymInner(TagBase tagBase, string groupName, CancellationToken cancellationToken)
    // {
    //     var synonymsGroup = await EnsureGroupExists(groupName, cancellationToken);
    //
    //     if (synonymsGroup.Synonyms.Contains(tagBase))
    //     {
    //         return new ErrorResponse($"The tag {tagBase} is already in synonyms group {synonymsGroup}");
    //     }
    //
    //     synonymsGroup.Synonyms.Add(tagBase);
    //     await _dbContext.SaveChangesAsync(cancellationToken);
    //
    //     return new None();
    // }
    //
    // private async Task<TagSynonymsGroup> EnsureGroupExists(string groupName, CancellationToken cancellationToken)
    // {
    //     var synonymsGroup = await _dbContext.TagSynonymsGroups
    //         .Include(group => group.Synonyms)
    //         .FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);
    //
    //     if (synonymsGroup is not null) return synonymsGroup;
    //
    //     synonymsGroup = new TagSynonymsGroup { Name = groupName, Synonyms = Array.Empty<TagBase>() };
    //
    //     _logger.LogInformation("Creating new synonym group with the name {SynonymGroupName}", groupName);
    //     var entry = await _dbContext.TagSynonymsGroups.AddAsync(synonymsGroup, cancellationToken);
    //     _ = await _dbContext.SaveChangesAsync(cancellationToken);
    //
    //     return entry.Entity;
    // }
}
