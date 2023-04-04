using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class MoveFileRequest : IRequest<OneOf<string, ErrorResponse>>
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }
}

[UsedImplicitly]
public class MoveFile : IRequestHandler<MoveFileRequest, OneOf<string, ErrorResponse>>
{
    private readonly ILogger<MoveFile> _logger;
    private readonly TagToolDbContext _dbContext;
    private readonly ICommonStoragePathProvider _commonStoragePathProvider;

    public MoveFile(ILogger<MoveFile> logger, TagToolDbContext dbContext, ICommonStoragePathProvider commonStoragePathProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _commonStoragePathProvider = commonStoragePathProvider;
    }

    public async Task<OneOf<string, ErrorResponse>> Handle(MoveFileRequest request, CancellationToken cancellationToken)
    {
        var (oldFulPath, newFullPath) = (request.OldFullPath, request.NewFullPath);

        if (newFullPath == "CommonStorage")
        {
            var oneOf = _commonStoragePathProvider.GetPathForFile(Path.GetFileName(request.OldFullPath.AsSpan()));
            if (oneOf.TryPickT0(out var newPath, out _))
            {
                newFullPath = newPath;
            }
            else
            {
                return new ErrorResponse($"Unable to get path for Common Storage for {newFullPath}");
            }
        }

        if (!Path.Exists(Path.GetDirectoryName(newFullPath)))
        {
            return new ErrorResponse("Specified destination folder does not exists.");
        }

        if (File.Exists(newFullPath))
        {
            return new ErrorResponse("File with the same filename already exists in the destination location.");
        }

        var moveResult = Move(oldFulPath, newFullPath);

        if (moveResult.TryPickT1(out var errorResponse, out _))
        {
            return errorResponse;
        }

        var taggedItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == "file" && item.UniqueIdentifier == oldFulPath, cancellationToken);

        return taggedItem is null
            ? newFullPath
            : await UpdateItem(taggedItem, newFullPath, cancellationToken);
    }

    private OneOf<string, ErrorResponse> Move(string oldFullPath, string newFullPath)
    {
        try
        {
            File.Move(oldFullPath, newFullPath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to move file from {OldPath} to {NewPath}", oldFullPath, newFullPath);
            return new ErrorResponse($"Unable to move a file from \"{oldFullPath}\" to \"{newFullPath}\".");
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
