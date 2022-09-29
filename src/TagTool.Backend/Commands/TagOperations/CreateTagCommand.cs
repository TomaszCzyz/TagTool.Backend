using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands.TagOperations;

public class CreateTagCommand : ICommand
{
    public required string TagName { get; set; }

    public async Task Execute()
    {
        await using var db = new TagContext();

        Console.WriteLine("Inserting a new tag");

        var tagInDb = db.Tags.FirstOrDefault(tag => tag.Name == TagName);

        if (tagInDb is null)
        {
            db.Tags.Add(new Tag {Name = TagName});
        }

        await db.SaveChangesAsync();
    }
}
