using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class MoveFolderRequest : IRequest<OneOf<string, ErrorResponse>>
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class MoveFolder : IRequestHandler<MoveFolderRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<MoveFolder> _logger;
    private readonly ICommonStoragePathProvider _commonStoragePathProvider;
    private readonly TagToolDbContext _dbContext;

    public MoveFolder(ILogger<MoveFolder> logger, ICommonStoragePathProvider commonStoragePathProvider, TagToolDbContext dbContext)
    {
        _logger = logger;
        _commonStoragePathProvider = commonStoragePathProvider;
        _dbContext = dbContext;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(MoveFolderRequest request, CancellationToken cancellationToken)
    {
        var (oldFullPath, newFullPath) = (request.OldFullPath, request.NewFullPath);

        if (newFullPath == "CommonStorage")
        {
            var oneOf = _commonStoragePathProvider.GetPathForFolder(oldFullPath);
            if (oneOf.TryPickT0(out var newPath, out _))
            {
                newFullPath = newPath;
            }
            else
            {
                return new ErrorResponse($"Unable to get path for Common Storage for {newFullPath}");
            }
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

        var taggedItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == "folder" && item.UniqueIdentifier == oldFullPath, cancellationToken);

        return taggedItem is null
            ? newFullPath
            : await UpdateItem(taggedItem, newFullPath, cancellationToken);
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

    private async Task<string> UpdateItem(TaggedItem taggedItem, string newFullPath, CancellationToken cancellationToken)
    {
        taggedItem.UniqueIdentifier = newFullPath;

        var entityEntry = _dbContext.TaggedItems.Update(taggedItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity.UniqueIdentifier;
    }
}
