using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

// todo: rename to Add..
public class AddSynonymRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string GroupName { get; init; }

    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public class AddSynonym : ICommandHandler<AddSynonymRequest, OneOf<string, ErrorResponse>>
{
    private readonly IAssociationManager _associationManager;
    private readonly TagToolDbContext _dbContext;

    public AddSynonym(IAssociationManager associationManager, TagToolDbContext dbContext)
    {
        _associationManager = associationManager;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(AddSynonymRequest request, CancellationToken cancellationToken)
    {
        var tagBase = await GetOrCreateTag(request.Tag, cancellationToken);

        var addSynonym = await _associationManager.AddSynonym(tagBase, request.GroupName, cancellationToken);

        return addSynonym.Match(_ => "successfully added synonym", response => response.Message);
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
