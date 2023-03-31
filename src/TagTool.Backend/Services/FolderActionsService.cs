using Grpc.Core;
using MediatR;

namespace TagTool.Backend.Services;

public class FolderActionsService : Backend.FolderActionsService.FolderActionsServiceBase
{
    private readonly IMediator _mediator;

    public FolderActionsService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override Task CanRenameFolder(
        IAsyncStreamReader<CanRenameFolderRequest> requestStream,
        IServerStreamWriter<CanRenameFolderReply> responseStream,
        ServerCallContext context)
    {
        return base.CanRenameFolder(requestStream, responseStream, context);
    }

    public override Task<RenameFolderReply> RenameFolder(RenameFolderRequest request, ServerCallContext context)
    {
        return base.RenameFolder(request, context);
    }

    public override Task<MoveFolderReply> MoveFolder(MoveFolderRequest request, ServerCallContext context)
    {
        return base.MoveFolder(request, context);
    }

    public override Task<DeleteFolderReply> DeleteFolder(DeleteFolderRequest request, ServerCallContext context)
    {
        return base.DeleteFolder(request, context);
    }

    public override Task<TagChildrenReply> TagChildren(TagChildrenRequest request, ServerCallContext context)
    {
        return base.TagChildren(request, context);
    }
}
