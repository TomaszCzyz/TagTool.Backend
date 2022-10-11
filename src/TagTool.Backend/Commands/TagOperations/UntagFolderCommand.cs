using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;

namespace TagTool.Backend.Commands.TagOperations;

public class UntagFolderCommand : ICommand
{
    private readonly string _path = null!;

    public required string Path
    {
        get => _path;
        init => _path = Directory.Exists(value) ? value : throw new IOException($"The directory {value} does not exists");
    }

    public required string TagName { get; init; }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var tag = db.Tags.Include(tag1 => tag1.Files).FirstOrDefault(t => t.Name == TagName);

        if (tag?.Files is null) return;

        var filesByFolder = tag.Files.Where(file => file.Path == Path);

        db.TrackedFiles.RemoveRange(filesByFolder);

        await db.SaveChangesAsync();
    }
}
