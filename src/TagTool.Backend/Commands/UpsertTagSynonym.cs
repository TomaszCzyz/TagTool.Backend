using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Commands;

public class UpsertTagSynonymRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string GroupName { get; init; }

    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public class UpsertTagSynonym : ICommandHandler<UpsertTagSynonymRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<UpsertTagSynonym> _logger;
    private readonly TagToolDbContext _dbContext;

    public UpsertTagSynonym(ILogger<UpsertTagSynonym> logger, TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(UpsertTagSynonymRequest request, CancellationToken cancellationToken)
    {
        // todo: CASE WHEN TAG ALREADY IS IN A GROUP
        var tag = await EnsureTagExists(request.Tag, cancellationToken);
        var synonymsGroup = await EnsureGroupExists(request.GroupName, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        synonymsGroup.TagsSynonyms.Add(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return "Successfully upserted tag to group.";
    }

    private async Task<TagBase> EnsureTagExists(TagBase tagBase, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == tagBase.FormattedName, cancellationToken);

        if (tag is not null) return tag;

        _logger.LogInformation("Creating tag {@TagBase} before upserting new tag association", tagBase);
        var entry = await _dbContext.Tags.AddAsync(tagBase, cancellationToken);

        return entry.Entity;
    }

    private async Task<TagSynonymsGroup> EnsureGroupExists(string groupName, CancellationToken cancellationToken)
    {
        var synonymsGroup = await _dbContext.TagSynonymsGroup.FirstOrDefaultAsync(group => group.Name == groupName, cancellationToken);

        if (synonymsGroup is not null) return synonymsGroup;

        synonymsGroup = new TagSynonymsGroup { Name = groupName, TagsSynonyms = new List<TagBase>() };

        _logger.LogInformation("Creating new synonym group with the name {SynonymGroupName}", groupName);
        var entry = await _dbContext.TagSynonymsGroup.AddAsync(synonymsGroup, cancellationToken);

        return entry.Entity;
    }
}
