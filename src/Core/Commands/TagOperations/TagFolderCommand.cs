using Microsoft.EntityFrameworkCore;
using TagTool.DbContext;
using TagTool.Extensions;
using TagTool.Models;
using File = TagTool.Models.File;

namespace TagTool.Commands.TagOperations;

public class TagFolderCommand : ICommand
{
    public string Path { get; set; }

    public string TagName { get; set; }

    public TagFolderCommand(string path, string tagName)
    {
        if (!Directory.Exists(path))
        {
            throw new IOException($"The directory {path} does not exists");
        }

        // todo: normalize path to allow simple string comparision
        Path = path;
        TagName = tagName;
    }

    public async Task Execute()
    {
        await using var db = new TagContext();

        var newTag = await db.Tags.FirstOrDefaultAsync(tag => tag.Name == TagName) ?? new Tag {Name = TagName};

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
