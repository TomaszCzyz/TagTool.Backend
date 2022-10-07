using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands.TagOperations;

public class CreateTagCommand : ICommand
{
    private readonly ILogger<CreateTagCommand> _logger;

    public required string TagName { get; init; }

    public CreateTagCommand(ILogger<CreateTagCommand> logger)
    {
        _logger = logger;
    }

    public async Task Execute()
    {
        await using var db = new TagContext();

        _logger.LogInformation("Executing command {CommandName}", nameof(CreateTagCommand));

        var tagInDb = db.Tags.FirstOrDefault(tag => tag.Name == TagName);

        if (tagInDb is not null)
        {
            _logger.LogDebug("Tag with name {TagName} already exists", TagName);
            return;
        }

        _logger.LogDebug("Inserting a new tag {TagName}", TagName);
        db.Tags.Add(new Tag { Name = TagName });

        await db.SaveChangesAsync();
    }
}
