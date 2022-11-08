using Grpc.Core;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Repositories;
using TagTool.Backend.Taggers;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Services;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ITagger<File> _fileTagger;
    private readonly ITagger<Folder> _folderTagger;
    private readonly ITagsRepo _tagsRepo;

    public TagService(ITagger<File> fileTagger, ITagger<Folder> folderTagger, ITagsRepo tagsRepo)
    {
        _fileTagger = fileTagger;
        _folderTagger = folderTagger;
        _tagsRepo = tagsRepo;
    }

    public override Task<CreateTagsReply> CreateTags(CreateTagsRequest request, ServerCallContext context)
    {
        var numInserted = _tagsRepo.AddIfNotExist(request.TagNames).Count;
        var result = new Result { Messages = { $"Inserted {numInserted} tag(s)" }, IsSuccess = true };

        return Task.FromResult(new CreateTagsReply { Results = { result } });
    }

    public override Task<DeleteTagsReply> DeleteTags(DeleteTagsRequest request, ServerCallContext context)
    {
        _tagsRepo.DeleteTags(request.TagNames);

        return Task.FromResult(new DeleteTagsReply());
    }

    public override async Task Tag(IAsyncStreamReader<TagRequest> requestStream, IServerStreamWriter<TagReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var tagRequest = requestStream.Current;
            if (tagRequest.FileInfo is { } fileInfo)
            {
                var file = new File { FullPath = fileInfo.Path };
                var isSuccess = _fileTagger.Tag(file, tagRequest.TagNames.ToArray());

                var untagReply = new TagReply { Result = new Result { IsSuccess = isSuccess is not null } };

                await responseStream.WriteAsync(untagReply, context.CancellationToken);
            }

            if (tagRequest.FolderInfo is { } folderInfo)
            {
                var folder = new Folder { FullPath = folderInfo.Path };
                var isSuccess = _folderTagger.Tag(folder, tagRequest.TagNames.ToArray());

                var untagReply = new TagReply { Result = new Result { IsSuccess = isSuccess is not null } };

                await responseStream.WriteAsync(untagReply, context.CancellationToken);
            }
        }
    }

    public override async Task Untag(IAsyncStreamReader<UntagRequest> requestStream, IServerStreamWriter<UntagReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
        {
            var tagRequest = requestStream.Current;
            if (tagRequest.FileInfo is { } fileInfo)
            {
                var file = new File { FullPath = fileInfo.Path };
                var isSuccess = _fileTagger.Untag(file, tagRequest.TagNames.ToArray());

                var untagReply = new UntagReply { Result = new Result { IsSuccess = isSuccess is not null } };

                await responseStream.WriteAsync(untagReply, context.CancellationToken);
            }

            if (tagRequest.FolderInfo is { } folderInfo)
            {
                var folder = new Folder { FullPath = folderInfo.Path };
                var isSuccess = _folderTagger.Untag(folder, tagRequest.TagNames.ToArray());

                var untagReply = new UntagReply { Result = new Result { IsSuccess = isSuccess is not null } };

                await responseStream.WriteAsync(untagReply, context.CancellationToken);
            }
        }
    }
}
