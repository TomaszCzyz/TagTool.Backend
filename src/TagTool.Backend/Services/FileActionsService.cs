using System.Diagnostics;
using Grpc.Core;
using MediatR;
using TagTool.Backend.DomainTypes;

namespace TagTool.Backend.Services;

public class FileActionsService : Backend.FileActionsService.FileActionsServiceBase
{
    private readonly IMediator _mediator;

    public FileActionsService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task CanRenameFile(
        IAsyncStreamReader<CanRenameFileRequest> requestStream,
        IServerStreamWriter<CanRenameFileReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var canRenameFileRequest = requestStream.Current;

            var directoryName = Path.GetDirectoryName(canRenameFileRequest.Item.Identifier);
            var query = new Queries.CanRenameFileRequest { NewFullPath = Path.Join(directoryName, canRenameFileRequest.NewFileName) };

            var response = await _mediator.Send(query);

            var reply = new CanRenameFileReply { Result = new Result { IsSuccess = response.CanRename, Messages = { response.Message } } };

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<RenameFileReply> RenameFile(RenameFileRequest request, ServerCallContext context)
    {
        var command = new Commands.RenameFileRequest { FullPath = request.Item.Identifier, NewFileName = request.NewFileName };

        var response = await _mediator.Send(command);

        var reply = new RenameFileReply { Result = new Result { IsSuccess = response.IsRenamed, Messages = { response.ErrorMessage } } };

        return reply;
    }

    public override async Task<MoveFileReply> MoveFile(MoveFileRequest request, ServerCallContext context)
    {
        var command = new Commands.MoveFileRequest { OldFullPath = request.Item.Identifier, NewFullPath = request.Destination };

        var response = await _mediator.Send(command);

        var reply = response.IsMoved
            ? new MoveFileReply { NewLocation = request.Destination }
            : new MoveFileReply { ErrorMessage = response.ErrorMessage };

        return reply;
    }

    public override Task<DeleteFileReply> DeleteFile(DeleteFileRequest request, ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    public override async Task<OpenFileReply> OpenFile(OpenFileRequest request, ServerCallContext context)
    {
        if (!File.Exists(request.FullFileName))
        {
            return await Task.FromResult(
                new OpenFileReply { Result = new Result { IsSuccess = false, Messages = { "Specified file does not exists." } } });
        }

        using var process = new Process();

        process.StartInfo.FileName = request.FullFileName;
        process.StartInfo.UseShellExecute = true;

        process.Start();

        return await Task.FromResult(new OpenFileReply { Result = new Result { IsSuccess = true } });
    }
}
