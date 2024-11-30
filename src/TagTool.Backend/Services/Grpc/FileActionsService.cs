using System.Diagnostics;
using Grpc.Core;
using MediatR;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Services.Grpc;

public class FileActionsService : Backend.FileActionsService.FileActionsServiceBase
{
    private readonly IMediator _mediator;
    private readonly ITaggableItemMapper _taggableItemMapper;

    public FileActionsService(IMediator mediator, ITaggableItemMapper taggableItemMapper)
    {
        _mediator = mediator;
        _taggableItemMapper = taggableItemMapper;
    }

    public override async Task CanRenameFile(
        IAsyncStreamReader<CanRenameFileRequest> requestStream,
        IServerStreamWriter<CanRenameFileReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var canRenameFileRequest = requestStream.Current;

            var directoryName = Path.GetDirectoryName(canRenameFileRequest.File.Path);
            var query = new Queries.CanRenameFileRequest { NewFullPath = Path.Join(directoryName, canRenameFileRequest.NewFileName) };

            var response = await _mediator.Send(query, context.CancellationToken);

            var reply = new CanRenameFileReply { Error = new Error { Message = response.Message } };

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<RenameFileReply> RenameFile(RenameFileRequest request, ServerCallContext context)
    {
        var command = new Commands.RenameFileRequest { FullPath = request.File.Path, NewFileName = request.NewFileName };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            newFullName => new RenameFileReply { NewFullName = newFullName },
            errorResponse => new RenameFileReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<MoveFileReply> MoveFile(MoveFileRequest request, ServerCallContext context)
    {
        var command = new Commands.MoveFileRequest { OldFullPath = request.File.Path, NewFullPath = request.Destination };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            successResponse => new MoveFileReply { NewLocation = successResponse.NewPath, InfoMessage = successResponse.AdditionalInfos },
            errorResponse => new MoveFileReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<DeleteFileReply> DeleteFile(DeleteFileRequest request, ServerCallContext context)
    {
        var command = new Commands.DeleteFileRequest { Path = request.File.Path };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            successResponse => new DeleteFileReply { DeletedFileFullName = successResponse.NewPath },
            errorResponse => new DeleteFileReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<OpenFileReply> OpenFile(OpenFileRequest request, ServerCallContext context)
    {
        if (!File.Exists(request.FullFileName))
        {
            return await Task.FromResult(new OpenFileReply { Error = new Error { Message = "Specified file does not exists." } });
        }

        using var process = new Process();

        process.StartInfo.FileName = request.FullFileName;
        process.StartInfo.UseShellExecute = true;

        process.Start();

        return await Task.FromResult(new OpenFileReply());
    }

    public override async Task<DetectNewItemsReply> DetectNewItems(DetectNewItemsRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new Commands.DetectNewItemsRequest());

        return result.Match(
            items => new DetectNewItemsReply { Items = { items.Select(_taggableItemMapper.MapToDto) } },
            _ => new DetectNewItemsReply { Error = new Error { Message = "NoWatchedLocations" } });
    }

    public override async Task<AddWatchedLocationReply> AddWatchedLocation(AddWatchedLocationRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new Commands.AddWatchedLocationRequest { Path = request.Path });

        return result.Match(
            _ => new AddWatchedLocationReply(),
            _ => new AddWatchedLocationReply { Error = new Error { Message = "Cannot access specified path for security reasons." } },
            _ => new AddWatchedLocationReply { Error = new Error { Message = "Path is too long." } },
            _ => new AddWatchedLocationReply { Error = new Error { Message = "Directory does not exist." } });
    }

    public override async Task<DeleteWatchedLocationReply> DeleteWatchedLocation(DeleteWatchedLocationRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new Commands.DeleteWatchedLocationRequest { Path = request.Path });

        return result.Match(
            _ => new DeleteWatchedLocationReply(),
            _ => new DeleteWatchedLocationReply { Error = new Error { Message = "Cannot access specified path for security reasons." } },
            _ => new DeleteWatchedLocationReply { Error = new Error { Message = "Path is too long." } },
            _ => new DeleteWatchedLocationReply { Error = new Error { Message = "Directory does not exist." } },
            _ => new DeleteWatchedLocationReply
            {
                Error = new Error { Message = "'Watched Location' has not been found in the user configuration." }
            });
    }
}
