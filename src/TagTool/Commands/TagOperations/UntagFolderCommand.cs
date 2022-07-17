using Microsoft.EntityFrameworkCore;
using TagTool.DbContext;
using TagTool.Models;

namespace TagTool.Commands.TagOperations;

public class UntagFolderCommand : ICommand
{
    public string Path { get; set; }

    public Tag Tag { get; set; }

    public UntagFolderCommand(string path, Tag tag)
    {
        if (!Directory.Exists(path))
        {
            throw new IOException($"The directory {path} does not exists");
        }

        Path = path;
        Tag = tag;
    }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var tag = db.Tags.Include(tag1 => tag1.Files).FirstOrDefault(t => t.Name == Tag.Name);

        if (tag?.Files is null) return;

        var filesByFolder = tag.Files.Where(file => file.Location == Path);

        db.Files.RemoveRange(filesByFolder);

        await db.SaveChangesAsync();
    }
}
