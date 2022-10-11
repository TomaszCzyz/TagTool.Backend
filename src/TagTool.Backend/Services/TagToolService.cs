// using Grpc.Core;
// using TagTool.Backend.Commands;
// using TagTool.Backend.Commands.TagOperations;
//
// namespace TagTool.Backend.Services;
//
// public class TagToolService : TagService.TagServiceBase
// {
//     private readonly ICommandInvoker _commandInvoker;
//     private readonly ILoggerFactory _loggerFactory;
//
//     public TagToolService(ICommandInvoker commandInvoker, ILoggerFactory loggerFactory)
//     {
//         _commandInvoker = commandInvoker;
//         _loggerFactory = loggerFactory;
//     }
//
//     public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
//     {
//         var logger = (ILogger<CreateTagCommand>)_loggerFactory.CreateLogger(typeof(CreateTagCommand));
//         var command = new CreateTagCommand(logger) { TagName = request.TagName };
//         await _commandInvoker.SetAndInvoke(command);
//
//         return new CreateTagReply { IsSuccess = true };
//     }
//
//     public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
//     {
//         var command = new DeleteTagCommand { TagName = request.TagName };
//         await _commandInvoker.SetAndInvoke(command);
//
//         return new DeleteTagReply { IsSuccess = true };
//     }
//
//     public override async Task<TagFilesInFolderReply> TagFilesInFolder(TagFilesInFolderRequest request, ServerCallContext context)
//     {
//         var command = new TagFolderCommand { Path = request.Path, TagName = request.TagName };
//         await _commandInvoker.SetAndInvoke(command);
//
//         return new TagFilesInFolderReply { IsSuccess = true };
//     }
//
//     public override async Task<UntagFolderReply> UntagFolder(UntagFolderRequest request, ServerCallContext context)
//     {
//         var command = new UntagFolderCommand { Path = request.Path, TagName = request.TagName };
//         await _commandInvoker.SetAndInvoke(command);
//
//         return new UntagFolderReply { IsSuccess = true };
//     }
//
//     public override async Task<UndoReply> Undo(UndoRequest request, ServerCallContext context)
//     {
//         await _commandInvoker.Undo();
//         return new UndoReply { IsSuccess = true };
//     }
//
//     public override async Task<RedoReply> Redo(RedoRequest request, ServerCallContext context)
//     {
//         await _commandInvoker.Redo();
//         return new RedoReply { IsSuccess = true };
//     }
// }
