using Grpc.Core;
using TagTool.Backend.Commands.TagOperations;

namespace TagTool.Backend.Services;

public class TagToolService : Backend.TagToolService.TagToolServiceBase
{
    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var command = new CreateTagCommand(request.TagName);
        await command.Execute();

        return new CreateTagReply { IsSuccess = true };
    }
}
