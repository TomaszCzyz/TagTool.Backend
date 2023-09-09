using System.Diagnostics;
using Grpc.Core;
using MediatR;
using TagTool.Backend.DomainTypes;

namespace TagTool.Backend.Services.Grpc;

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

    public override Task<DeleteFileReply> DeleteFile(DeleteFileRequest request, ServerCallContext context) => throw new NotImplementedException();

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
}
