using Grpc.Core;
using TagTool.Backend.Commands;
using TagTool.Backend.Commands.TagOperations;

namespace TagTool.Backend.Services;

public class TagToolService : TagService.TagServiceBase
{
    private readonly ICommandInvoker _commandInvoker;

    public TagToolService(ICommandInvoker commandInvoker)
    {
        _commandInvoker = commandInvoker;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var command = new CreateTagCommand { TagName = request.TagName };
        await _commandInvoker.SetAndInvoke(command);

        return new CreateTagReply { IsSuccess = true };
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var command = new DeleteTagCommand(request.TagName);
        await _commandInvoker.SetAndInvoke(command);

        return new DeleteTagReply { IsSuccess = true };
    }

    public override async Task<TagFolderReply> TagFolder(TagFolderRequest request, ServerCallContext context)
    {
        var command = new TagFolderCommand(request.Path, request.TagName);
        await _commandInvoker.SetAndInvoke(command);

        return new TagFolderReply { IsSuccess = true };
    }

    public override async Task<UntagFolderReply> UntagFolder(UntagFolderRequest request, ServerCallContext context)
    {
        var command = new UntagFolderCommand(request.Path, request.TagName);
        await _commandInvoker.SetAndInvoke(command);

        return new UntagFolderReply { IsSuccess = true };
    }

    public override async Task<UndoReply> Undo(UndoRequest request, ServerCallContext context)
    {
        await _commandInvoker.Undo();
        return new UndoReply { IsSuccess = true };
    }

    public override async Task<RedoReply> Redo(RedoRequest request, ServerCallContext context)
    {
        await _commandInvoker.Redo();
        return new RedoReply { IsSuccess = true };
    }
}
