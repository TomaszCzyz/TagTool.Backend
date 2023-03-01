using System.Diagnostics;
using Grpc.Core;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Repositories;
using TagTool.Backend.Repositories.Dtos;
using TagTool.Backend.Taggers;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Services;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ITagger<File> _fileTagger;
    private readonly ITagger<Folder> _folderTagger;
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;

    public TagService(ITagger<File> fileTagger, ITagger<Folder> folderTagger, ITagsRepo tagsRepo, ITaggedItemsRepo taggedItemsRepo)
    {
        _fileTagger = fileTagger;
        _folderTagger = folderTagger;
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
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

    public override Task<TagReply> Tag(TagRequest request, ServerCallContext context)
    {
        if (request.FileInfo is { } fileInfo)
        {
            var file = new File { FullPath = fileInfo.Path };
            var isSuccess = _fileTagger.Tag(file, request.TagNames.ToArray());

            return Task.FromResult(new TagReply { Result = new Result { IsSuccess = isSuccess is not null } });
        }

        if (request.FolderInfo is { } folderInfo)
        {
            var folder = new Folder { FullPath = folderInfo.Path };
            var isSuccess = _folderTagger.Tag(folder, request.TagNames.ToArray());

            return Task.FromResult(new TagReply { Result = new Result { IsSuccess = isSuccess is not null } });
        }

        return Task.FromResult(new TagReply { Result = new Result { IsSuccess = false } });
    }

    public override Task<UntagReply> Untag(UntagRequest request, ServerCallContext context)
    {
        if (request.FileInfo is { } fileInfo)
        {
            var file = new File { FullPath = fileInfo.Path };
            var isSuccess = _fileTagger.Untag(file, request.TagNames.ToArray());

            return Task.FromResult(new UntagReply { Result = new Result { IsSuccess = isSuccess is not null } });
        }

        if (request.FolderInfo is { } folderInfo)
        {
            var folder = new Folder { FullPath = folderInfo.Path };
            var isSuccess = _folderTagger.Untag(folder, request.TagNames.ToArray());

            return Task.FromResult(new UntagReply { Result = new Result { IsSuccess = isSuccess is not null } });
        }

        return Task.FromResult(new UntagReply { Result = new Result { IsSuccess = false } });
    }

    public override async Task GetItems(
        GetItemsRequest request,
        IServerStreamWriter<GetItemsResponse> responseStream,
        ServerCallContext context)
    {
        var tags = request.TagNames.ToArray();
        var taggedItems = _taggedItemsRepo.FindByTags(tags);

        foreach (var item in taggedItems)
        {
            var tagNames = item.Tags.Select(dto => dto.Name);
            var getItemsResponse = item switch
            {
                FileDto fileDto
                    => new GetItemsResponse { FileInfo = new FileDescription { Path = fileDto.FullPath }, TagNames = { tagNames } },
                FolderDto folderDto
                    => new GetItemsResponse { FolderInfo = new FolderDescription { Path = folderDto.FullPath }, TagNames = { tagNames } },
                _ => throw new UnreachableException()
            };

            await responseStream.WriteAsync(getItemsResponse);
        }
    }

    public override Task<GetItemInfoReply> GetItemInfo(GetItemInfoRequest request, ServerCallContext context)
    {
        TaggedItemDto? item;
        switch (request.Type)
        {
            case "file":
                var fileDto = new FileDto { FullPath = request.ItemIdentifier };
                item = _taggedItemsRepo.FindOne(fileDto);
                break;
            case "folder":
                var folderDto = new FolderDto { FullPath = request.ItemIdentifier };
                item = _taggedItemsRepo.FindOne(folderDto);
                break;
            default:
                throw new UnreachableException();
        }

        if (item is null || item.Tags.Count == 0)
        {
            return Task.FromResult(new GetItemInfoReply());
        }

        var tagNames = item.Tags.Select(dto => dto.Name).ToArray();

        return Task.FromResult(new GetItemInfoReply { Tags = { tagNames } });
    }
}
