using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class AddChildRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required TagBase ChildTag { get; init; }

    public required TagBase ParentTag { get; init; }
}

[UsedImplicitly]
public class AddChild : ICommandHandler<AddChildRequest, OneOf<string, ErrorResponse>>
{
    private readonly ITagsRelationsManager _tagsRelationsManager;
    private readonly TagToolDbContext _dbContext;

    public AddChild(ITagsRelationsManager tagsRelationsManager, TagToolDbContext dbContext)
    {
        _tagsRelationsManager = tagsRelationsManager;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(AddChildRequest request, CancellationToken cancellationToken)
    {
        var (childTag, parentTag) = await GetOrCreateTags(request.ChildTag, request.ParentTag, cancellationToken);

        var addChild = await _tagsRelationsManager.AddChild(childTag, parentTag, cancellationToken);

        return addChild.Match(_ => "successfully added child", response => response.Message);
    }

    private async Task<(TagBase TagBase1, TagBase TagBase2)> GetOrCreateTags(TagBase tag1, TagBase tag2, CancellationToken cancellationToken)
    {
        // todo: make it with one query
        var tagBase1 = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag1.FormattedName, cancellationToken);
        var tagBase2 = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag2.FormattedName, cancellationToken);

        return (tagBase1, tagBase2) switch
        {
            (not null, not null) => (tagBase1, tagBase2),
            (null, not null) => (await CreateTag(tag1, cancellationToken), tagBase2),
            (not null, null) => (tagBase1, await CreateTag(tag2, cancellationToken)),
            (null, null) => await CreateTags(tag1, tag2, cancellationToken)
        };
    }

    private async Task<TagBase> CreateTag(TagBase tag, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.Tags.AddAsync(tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity;
    }

    private async Task<(TagBase TagBase1, TagBase TagBase2)> CreateTags(TagBase tag1, TagBase tag2, CancellationToken cancellationToken)
    {
        var entry1 = await _dbContext.Tags.AddAsync(tag1, cancellationToken);
        var entry2 = await _dbContext.Tags.AddAsync(tag2, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (entry1.Entity, entry2.Entity);
    }
}
