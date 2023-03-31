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

            var query = new Queries.CanRenameFileRequest
            {
                NewFullPath = Path.Join(Path.GetDirectoryName(canRenameFileRequest.Item.Identifier), canRenameFileRequest.NewFileName)
            };

            var result = await _mediator.Send(query);

            var canRenameFileReply = result.CanRename
                ? new CanRenameFileReply { Result = new Result { IsSuccess = true } }
                : new CanRenameFileReply { Result = new Result { IsSuccess = false, Messages = { result.Message } } };

            await responseStream.WriteAsync(canRenameFileReply);
        }
    }

    public override async Task<RenameFileReply> RenameFile(RenameFileRequest request, ServerCallContext context)
    {
        var command = new Commands.RenameFileRequest { FullPath = request.Item.Identifier, NewFileName = request.NewFileName };

        var response = await _mediator.Send(command);

        var reply = response.IsRenamed
            ? new RenameFileReply { Result = new Result { IsSuccess = true } }
            : new RenameFileReply { Result = new Result { IsSuccess = false, Messages = { response.ErrorMessage } } };

        return reply;
    }
}
