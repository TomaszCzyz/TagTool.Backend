using Microsoft.EntityFrameworkCore;
using TagTool.DbContext;
using TagTool.Models;
using File = TagTool.Models.File;

namespace TagTool.Commands;

public class TagFolderCommand : ICommand
{
    public string Path { get; init; }

    public Tag Tag { get; init; }

    public TagFolderCommand(string path, Tag tag)
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

        foreach (var fullFilePath in Directory.EnumerateFiles(Path))
        {
            var fileInfo = new FileInfo(fullFilePath);
            var fileName = fileInfo.Name;
            var fileLength = fileInfo.Length;

            // todo: optimization - make process run in batches
            var file = await db.Files.FirstOrDefaultAsync(file => file.Name == fileName && file.Length == fileLength);

            if (file is null)
            {
                file = new File(fileInfo, Tag);

                db.Files.Add(file);
            }
            else
            {
                db.Files.Update(file);
            }

            await db.SaveChangesAsync();
        }
    }
}
