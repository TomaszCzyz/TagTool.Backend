using Grpc.Core;

namespace TagTool.Backend.Services;

public class TagSearchService : Backend.TagSearchService.TagSearchServiceBase
{
    public override Task FindTags(
        IAsyncStreamReader<FindTagsRequest> requestStream,
        IServerStreamWriter<FindTagsReply> responseStream,
        ServerCallContext context)
    {
        
        return base.FindTags(requestStream, responseStream, context);
    }
}
