using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public record SuccessResponse(string NewPath, string? AdditionalInfos);

public class MoveFileRequest : ICommand<OneOf<SuccessResponse, ErrorResponse>>, IReversible
{
    public required string OldFullPath { get; init; }

    public required string NewFullPath { get; init; }

    public IReversible GetReverse() => new MoveFileRequest { NewFullPath = OldFullPath, OldFullPath = NewFullPath };
}

[UsedImplicitly]
public class MoveFile : ICommandHandler<MoveFileRequest, OneOf<SuccessResponse, ErrorResponse>>
{
    private readonly ILogger<MoveFile> _logger;
    private readonly ITagToolDbContext _dbContext;
    private readonly ICommonStorage _commonStorage;

    public MoveFile(ILogger<MoveFile> logger, ITagToolDbContext dbContext, ICommonStorage commonStorage)
    {
        _logger = logger;
        _dbContext = dbContext;
        _commonStorage = commonStorage;
    }

    public async Task<OneOf<SuccessResponse, ErrorResponse>> Handle(MoveFileRequest request, CancellationToken cancellationToken)
    {
        var (oldFulPath, newFullPath) = (request.OldFullPath, request.NewFullPath);
        string? additionalInfos = null;

        // todo: replace magick string
        if (newFullPath == "CommonStorage")
        {
            var oneOf = _commonStorage.GetPath(request.OldFullPath, false);
            if (!oneOf.TryPickT0(out var storageInfo, out var error))
            {
                return error;
            }

            newFullPath = storageInfo.Path;
            additionalInfos = storageInfo.SimilarFiles;
        }

        // todo: these validation are duplicated in case we store file in a CommonStorage
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

        var taggedItem = await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == oldFulPath, cancellationToken);

        if (taggedItem is not null)
        {
            newFullPath = await UpdateItem(taggedItem, newFullPath, cancellationToken);
        }

        return new SuccessResponse(newFullPath, additionalInfos);
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

    private async Task<string> UpdateItem(TaggableFile taggedItem, string newFullPath, CancellationToken cancellationToken)
    {
        taggedItem.Path = newFullPath;

        var entityEntry = _dbContext.TaggableFiles.Update(taggedItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity.Path;
    }
}
