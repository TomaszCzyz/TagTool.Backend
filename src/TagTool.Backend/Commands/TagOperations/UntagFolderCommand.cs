using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands.TagOperations;

public class UntagFolderCommand : ICommand
{
    public string Path { get; set; }

    public string TagName { get; set; }

    public UntagFolderCommand(string path, string tagName)
    {
        if (!Directory.Exists(path))
        {
            throw new IOException($"The directory {path} does not exists");
        }

        Path = path;
        TagName = tagName;
    }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var tag = db.Tags.Include(tag1 => tag1.Files).FirstOrDefault(t => t.Name == TagName);

        if (tag?.Files is null) return;

        var filesByFolder = tag.Files.Where(file => file.Location == Path);

        db.Files.RemoveRange(filesByFolder);

        await db.SaveChangesAsync();
    }
}
