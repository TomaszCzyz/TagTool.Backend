using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class SetTagNamingConventionCommand : ICommand<OneOf<None, ErrorResponse>>
{
    public required Models.Options.NamingConvention NewNamingConvention { get; init; }

    public bool ApplyToExisting { get; init; } = true;
}

[UsedImplicitly]
public class SetTagNamingConvention : ICommandHandler<SetTagNamingConventionCommand, OneOf<None, ErrorResponse>>
{
    private readonly ILogger<SetTagNamingConvention> _logger;
    private readonly TagToolDbContext _dbContext;
    private readonly ITagNameProvider _tagNameProvider;

    public SetTagNamingConvention(
        ILogger<SetTagNamingConvention> logger,
        TagToolDbContext dbContext,
        ITagNameProvider tagNameProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _tagNameProvider = tagNameProvider;
    }

    // todo: expand functionality to the rest of tag types
    public async Task<OneOf<None, ErrorResponse>> Handle(SetTagNamingConventionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting naming convention {NamingConvention}", request.NewNamingConvention);
        // todo: set naming convention in a configuration

        if (!request.ApplyToExisting)
        {
            return new None();
        }

        _logger.LogInformation("Applying naming convention {NamingConvention} to existing tags", request.NewNamingConvention);

        await foreach (var tag in _dbContext.NormalTags.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            tag.Name = _tagNameProvider.GetName(tag.Name, request.NewNamingConvention);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new None();
    }
}
