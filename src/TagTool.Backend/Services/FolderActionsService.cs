using Grpc.Core;
using MediatR;
using TagTool.Backend.DomainTypes;

namespace TagTool.Backend.Services;

public class FolderActionsService : Backend.FolderActionsService.FolderActionsServiceBase
{
    private readonly IMediator _mediator;

    public FolderActionsService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task CanRenameFolder(
        IAsyncStreamReader<CanRenameFolderRequest> requestStream,
        IServerStreamWriter<CanRenameFolderReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var canRenameFolderRequest = requestStream.Current;

            var query = new Queries.CanRenameFolderRequest
            {
                NewFullPath = Path.Join(Path.GetDirectoryName(canRenameFolderRequest.FullName), canRenameFolderRequest.NewFolderName)
            };

            var response = await _mediator.Send(query);

            var reply = new CanRenameFolderReply { Result = new Result { IsSuccess = response.CanRename, Messages = { response.Message } } };

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<RenameFolderReply> RenameFolder(RenameFolderRequest request, ServerCallContext context)
    {
        var command = new Commands.RenameFolderRequest { FullPath = request.FullName, NewFolderName = request.NewFolderName };

        var response = await _mediator.Send(command);

        var reply = new RenameFolderReply { Result = new Result { IsSuccess = response.IsRenamed, Messages = { response.ErrorMessage } } };

        return reply;
    }

    public override async Task<MoveFolderReply> MoveFolder(MoveFolderRequest request, ServerCallContext context)
    {
        var command = new Commands.MoveFolderRequest { OldFullPath = request.FullName, NewFullPath = request.Destination };

        var response = await _mediator.Send(command);

        var reply = response.IsMoved
            ? new MoveFolderReply { NewLocation = request.Destination }
            : new MoveFolderReply { ErrorMessage = response.ErrorMessage };

        return reply;
    }

    public override Task<DeleteFolderReply> DeleteFolder(DeleteFolderRequest request, ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    public override async Task<TagChildrenReply> TagChildren(TagChildrenRequest request, ServerCallContext context)
    {
        var command = new Commands.TagFolderChildrenRequest
        {
            RootFolder = request.FullName,
            TagName = request.TagName,
            Depth = request.Depth,
            TagFilesOnly = request.TagOnlyFiles
        };
        var response = await _mediator.Send(command);

        var reply = new TagChildrenReply { Result = new Result { IsSuccess = response.IsSuccess, Messages = { response.ErrorMessage } } };

        return reply;
    }
}
