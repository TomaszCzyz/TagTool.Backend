using Grpc.Core;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.New;
using TagTool.Backend.Repositories;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Services;

public class NewTagService : New.NewTagService.NewTagServiceBase
{
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;
    private readonly ITaggersManager _taggersManager;

    public NewTagService(
        ITagsRepo tagsRepo,
        ITaggedItemsRepo taggedItemsRepo,
        ITaggersManager taggersManager)
    {
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
        _taggersManager = taggersManager;
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
        // switch (request.Item.ItemType)
        // {
        //     case "file":
        //         new File { FullPath = request.Item.Identifier };
        // }
        var taggable = request.Item.ItemType switch
        {
            "file" => (ITaggable)new File { FullPath = request.Item.Identifier },
            "folder" => new Folder { FullPath = request.Item.Identifier },
            _ => null
        };

        if (taggable is null)
        {
            return Task.FromResult(new TagItemReply { ErrorMessage = "Unrecognized ItemType." });
        }

        var taggedItem = _taggersManager.Tag(taggable, request.TagName);

        if (taggedItem is null)
        {
            return Task.FromResult(new TagItemReply { ErrorMessage = $"Unable to tag item{request.Item} with tag {request.TagName}." });
        }

        var tagNames = taggedItem.Tags.Select(tag => tag.Name).ToArray();
        return Task.FromResult(new TagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { tagNames } } });
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
