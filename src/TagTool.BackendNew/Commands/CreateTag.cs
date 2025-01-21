using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Commands;

using Response = OneOf<TagBase, Error<string>>;

public class CreateTag : ICommand<Response>
{
    public required string Text { get; init; }
}

[UsedImplicitly]
public class CreateTagCommandHandler : ICommandHandler<CreateTag, Response>
{
    private readonly ILogger<CreateTagCommandHandler> _logger;
    private readonly ITagToolDbContext _dbContext;

    public CreateTagCommandHandler(ILogger<CreateTagCommandHandler> logger, ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Response> Handle(CreateTag request, CancellationToken cancellationToken)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Text == request.Text, cancellationToken);

        if (tag is not null)
        {
            return new Error<string>($"Tag with text {request.Text} already exists.");
        }

        _logger.LogInformation("Creating new tag {@Tag}", request.Text);

        var newTag = new TagBase { Text = request.Text };

        _dbContext.Tags.Add(newTag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newTag;
    }
}
