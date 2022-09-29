using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using File = TagTool.Backend.Models.File;

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

        foreach (var fileInfo in Directory.EnumerateFiles(Path).Select(s => new FileInfo(s)))
        {
            var fileName = fileInfo.Name;
            var fileLength = fileInfo.Length;

            var fileInDb = await db.Files // todo: optimization - make process run in batches
                .Include(file => file.Tags)
                .FirstOrDefaultAsync(file => file.Name == fileName && file.Length == fileLength);

            if (fileInDb is not null)
            {
                fileInDb.Tags.AddIfNotExists(newTag);

                db.Files.Update(fileInDb);
            }
            else
            {
                var newFile = (File)fileInfo;
                newFile.Tags.AddIfNotExists(newTag);

                db.Files.Add(newFile);
            }

            await db.SaveChangesAsync();
        }
    }
}
