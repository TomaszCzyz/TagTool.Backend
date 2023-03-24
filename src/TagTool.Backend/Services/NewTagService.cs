using Grpc.Core;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.New;
using TagTool.Backend.Repositories;
using TagTool.Backend.Taggers;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Services;

public class NewTagService : New.NewTagService.NewTagServiceBase
{
    private readonly ITagger<File> _fileTagger;
    private readonly ITagger<Folder> _folderTagger;
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;

    public NewTagService(ITagger<File> fileTagger, ITagger<Folder> folderTagger, ITagsRepo tagsRepo, ITaggedItemsRepo taggedItemsRepo)
    {
        _fileTagger = fileTagger;
        _folderTagger = folderTagger;
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
    }

    public override Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var exists = _tagsRepo.Exists(request.TagName);
        if (exists)
        {
            return Task.FromResult(new CreateTagReply { ErrorMessage = $"Tag {request.TagName} already exists." });
        }

        var tagDto = _tagsRepo.Insert(request.TagName);

        return Task.FromResult(new CreateTagReply { CreatedTagName = tagDto.Name });
    }

    public override Task<New.DeleteTagReply> DeleteTag(New.DeleteTagRequest request, ServerCallContext context)
    {
        var exists = _tagsRepo.Exists(request.TagName);
        if (!exists)
        {
            return Task.FromResult(new New.DeleteTagReply { ErrorMessage = $"Tag {request.TagName} does not exists." });
        }

        if (!request.DeleteUsedToo && _taggedItemsRepo.FindByTags(new[] { request.TagName }).Any())
        {
            return Task.FromResult(
                new New.DeleteTagReply
                {
                    ErrorMessage = $"Tag {request.TagName} is in use and it was not deleted. " +
                                   $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag."
                });
        }

        var isDeleted = _tagsRepo.DeleteTag(request.TagName);
        var replay = isDeleted
            ? new New.DeleteTagReply { DeletedTagName = request.TagName }
            : new New.DeleteTagReply { ErrorMessage = $"Unable to delete tag {request.TagName}" };

        return Task.FromResult(replay);
    }

    public override Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        // var taggable = request.Item.ItemType switch
        // {
        //     "file" => (ITaggable)new File { FullPath = request.Item.Identifier },
        //     "folder" => (ITaggable)new Folder { FullPath = request.Item.Identifier },
        //     _ => null
        // };
        //
        // if (taggable is null)
        // {
        //     return Task.FromResult(new TagItemReply { ErrorMessage = "Unrecognized ItemType." });
        // }
        //
        // var isSuccess = _fileTagger.Tag(taggable, request.TagName);

        return Task.FromResult(new TagItemReply());
    }

    public override Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        return base.UntagItem(request, context);
    }

    public override Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        return base.GetItem(request, context);
    }

    public override Task GetItemsByTags(New.GetItemsRequest request, IServerStreamWriter<GetItemsReply> responseStream,
        ServerCallContext context)
    {
        return base.GetItemsByTags(request, responseStream, context);
    }

    public override Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        return base.DoesItemExists(request, context);
    }

    public override Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        return base.DoesTagExists(request, context);
    }

    public override Task SearchTags(SearchTagsRequest request, IServerStreamWriter<SearchTagsReply> responseStream, ServerCallContext context)
    {
        return base.SearchTags(request, responseStream, context);
    }
}
