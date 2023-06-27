using Grpc.Core;
using MediatR;
using TagTool.Backend.Commands;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models.Mappers;

namespace TagTool.Backend.Services.Grpc;

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
                NewFullPath = Path.Join(Path.GetDirectoryName(canRenameFolderRequest.Folder.Path), canRenameFolderRequest.NewFolderName)
            };

            var response = await _mediator.Send(query, context.CancellationToken);

            var reply = response.CanRename
                ? new CanRenameFolderReply()
                : new CanRenameFolderReply { Error = new Error { Message = response.Message } };

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<RenameFolderReply> RenameFolder(RenameFolderRequest request, ServerCallContext context)
    {
        var command = new Commands.RenameFolderRequest { FullPath = request.Folder.Path, NewFolderName = request.NewFolderName };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            newFullName => new RenameFolderReply { NewFullName = newFullName },
            errorResponse => new RenameFolderReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<MoveFolderReply> MoveFolder(MoveFolderRequest request, ServerCallContext context)
    {
        var command = new Commands.MoveFolderRequest { OldFullPath = request.Folder.Path, NewFullPath = request.Destination };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            successResponse => new MoveFolderReply { NewLocation = successResponse.NewPath },
            errorResponse => new MoveFolderReply { ErrorMessage = errorResponse.Message });
    }

    public override Task<DeleteFolderReply> DeleteFolder(DeleteFolderRequest request, ServerCallContext context)
        => throw new NotImplementedException();

    public override async Task<TagChildrenReply> TagChildren(TagChildrenRequest request, ServerCallContext context)
    {
        var tagBase = TagMapper.MapToDomain(request.Tag);

        var command = new TagFolderChildrenRequest
        {
            RootFolder = request.Folder.Path,
            Tag = tagBase,
            Depth = request.Depth,
            TagFilesOnly = request.TagOnlyFiles
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            message => new TagChildrenReply { SuccessMessage = message },
            errorResponse => new TagChildrenReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<UntagChildrenReply> UntagChildren(UntagChildrenRequest request, ServerCallContext context)
    {
        var tagBase = TagMapper.MapToDomain(request.Tag);

        var command = new UntagFolderChildrenRequest
        {
            RootFolder = request.Folder.Path,
            Tag = tagBase,
            Depth = request.Depth,
            TagFilesOnly = request.TagOnlyFiles
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            message => new UntagChildrenReply { SuccessMessage = message },
            errorResponse => new UntagChildrenReply { ErrorMessage = errorResponse.Message });
    }
}
