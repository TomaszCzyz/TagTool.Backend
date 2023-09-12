using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class RemoveSynonymRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string GroupName { get; init; }

    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public class RemoveSynonym : ICommandHandler<RemoveSynonymRequest, OneOf<string, ErrorResponse>>
{
    private readonly ITagsRelationsManager _tagsRelationsManager;
    private readonly TagToolDbContext _dbContext;

    public RemoveSynonym(ITagsRelationsManager tagsRelationsManager, TagToolDbContext dbContext)
    {
        _tagsRelationsManager = tagsRelationsManager;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(RemoveSynonymRequest request, CancellationToken cancellationToken)
    {
        var tagBase = await GetOrCreateTag(request.Tag, cancellationToken);

        var removeSynonym = await _tagsRelationsManager.RemoveSynonym(tagBase, request.GroupName, cancellationToken);

        return removeSynonym.Match(_ => "successfully removed synonym", response => response.Message);
    }

    private async Task<TagBase> GetOrCreateTag(TagBase tag, CancellationToken cancellationToken)
    {
        var tagBase = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        if (tagBase is not null)
        {
            return tagBase;
        }

        var entry = await _dbContext.Tags.AddAsync(tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity;
    }
}
