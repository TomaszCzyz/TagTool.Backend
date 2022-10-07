using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands.TagOperations;

public class TagFolderCommand : ICommand
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

        var newTag = await db.Tags.FirstOrDefaultAsync(tag => tag.Name == TagName) ?? new Tag { Name = TagName };

        foreach (var trackedFile in Directory.EnumerateFiles(Path).Select(s => new TrackedFile(s)))
        {
            var file = db.TrackedFiles.AddIfNotExists(trackedFile, file => file.Name == trackedFile.Name && file.Length == trackedFile.Length);

            file.Tags.AddIfNotExists(newTag);
        }

        await db.SaveChangesAsync();
    }
}
