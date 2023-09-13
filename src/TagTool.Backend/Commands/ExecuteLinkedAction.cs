using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class ExecuteLinkedRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required TaggableItem Item { get; init; }
}

[UsedImplicitly]
public class ExecuteLinked : ICommandHandler<ExecuteLinkedRequest, OneOf<string, ErrorResponse>>
{
    private readonly TagToolDbContext _dbContext;

    public ExecuteLinked(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(ExecuteLinkedRequest request, CancellationToken cancellationToken)
    {
        switch (request.Item)
        {
            case TaggableFolder folder:
                // todo: it only works on windows
                Process.Start("explorer.exe", folder.Path);
                break;
            case TaggableFile file when !File.Exists(file.Path):
                return new ErrorResponse($"File {file.Path} does not exist.");
            case TaggableFile file:
                using (var process = new Process())
                {
                    process.StartInfo.FileName = file.Path;
                    process.StartInfo.UseShellExecute = true;

                    process.Start();
                }

                break;
            default:
                return new ErrorResponse($"No match taggable item type found for item {request.Item}");
        }

        TaggableItem existingTaggableItem = request.Item switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles
                    .Include(file => file.Tags)
                    .FirstAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders
                    .Include(folder => folder.Tags)
                    .FirstAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new UnreachableException()
        };

        existingTaggableItem.Popularity++;
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return $"Successfully executed linked action for item {request.Item}";
    }
}
