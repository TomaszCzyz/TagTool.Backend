using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class MoveFolderRequest : ICommand<OneOf<SuccessResponse, ErrorResponse>>, IReversible
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }

    public IReversible GetReverse() => new MoveFolderRequest { NewFullPath = OldFullPath, OldFullPath = NewFullPath };
}

[UsedImplicitly]
public class MoveFolder : ICommandHandler<MoveFolderRequest, OneOf<SuccessResponse, ErrorResponse>>
{
    private readonly ILogger<MoveFolder> _logger;
    private readonly ICommonStorage _commonStorage;
    private readonly TagToolDbContext _dbContext;

    public MoveFolder(ILogger<MoveFolder> logger, TagToolDbContext dbContext, ICommonStorage commonStorage)
    {
        _logger = logger;
        _dbContext = dbContext;
        _commonStorage = commonStorage;
    }

    public async Task<OneOf<SuccessResponse, ErrorResponse>> Handle(MoveFolderRequest request, CancellationToken cancellationToken)
    {
        var (oldFullPath, newFullPath) = (request.OldFullPath, request.NewFullPath);

        if (newFullPath == "CommonStorage")
        {
            var oneOf = _commonStorage.GetPath(request.OldFullPath, false);
            if (!oneOf.TryPickT0(out var storageInfo, out var error))
            {
                return error;
            }

            newFullPath = storageInfo.Path;
            // additionalInfos = storageInfo.SimilarFiles;
        }

        if (Directory.Exists(newFullPath))
        {
            return new ErrorResponse("Folder with the same filename already exists in the destination location.");
        }

        var moveResult = Move(oldFullPath, newFullPath);

        if (moveResult.TryPickT1(out var errorResponse, out _))
        {
            return errorResponse;
        }

        var taggedItem = await _dbContext.TaggableFolders.FirstOrDefaultAsync(folder => folder.Path == oldFullPath, cancellationToken);

        if (taggedItem is not null)
        {
            newFullPath = await UpdateItem(taggedItem, newFullPath, cancellationToken);
        }

        return new SuccessResponse(newFullPath, null);
    }

    private OneOf<string, ErrorResponse> Move(string oldFullPath, string newFullPath)
    {
        try
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to move folder from {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new ErrorResponse($"Unable to move a folder from \"{oldFullPath}\" to \"{newFullPath}\".");
        }

        return newFullPath;
    }

    private async Task<string> UpdateItem(TaggableFolder taggedItem, string newFullPath, CancellationToken cancellationToken)
    {
        taggedItem.Path = newFullPath;

        var entityEntry = _dbContext.TaggableFolders.Update(taggedItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity.Path;
    }
}
