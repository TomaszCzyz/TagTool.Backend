using TagTool.DbContext;

namespace TagTool.Commands.TagOperations;

public class DeleteTagCommand : ICommand
{
    public string TagName { get; set; }

    public DeleteTagCommand(string tagName)
    {
        TagName = tagName;
    }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var tagToDelete = db.Tags.First(tag => tag.Name == TagName);
        db.Tags.Remove(tagToDelete);

        await db.SaveChangesAsync();
    }
}
