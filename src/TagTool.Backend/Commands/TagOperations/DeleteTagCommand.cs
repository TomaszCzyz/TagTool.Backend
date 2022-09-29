using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands.TagOperations;

public class DeleteTagCommand : ICommand
{
    public required string TagName { get; init; }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var tagToDelete = db.Tags.First(tag => tag.Name == TagName);
        db.Tags.Remove(tagToDelete);

        await db.SaveChangesAsync();
    }
}
