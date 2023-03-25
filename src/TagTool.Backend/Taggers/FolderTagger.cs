using TagTool.Backend.Models;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Repositories;
using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Taggers;

public class FolderTagger : ITagger<Folder>
{
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;

    public FolderTagger(ITagsRepo tagsRepo, ITaggedItemsRepo taggedItemsRepo)
    {
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
    }

    public TaggedItem? Tag(Folder item, string[] tagNames)
    {
        var tags = _tagsRepo.AddIfNotExist(tagNames);
        var folderDto = _taggedItemsRepo.FindOne(new FolderDto { FullPath = item.FullPath });

        var isSuccess = false;

        if (folderDto is not null)
        {
            var newTags = tags.Except(folderDto.Tags, TagDto.NameComparer).ToArray();
            if (newTags.Length != 0)
            {
                var updatedDto = new FolderDto
                {
                    Id = folderDto.Id,
                    FullPath = folderDto.FullPath,
                    Tags = folderDto.Tags.Concat(newTags).ToList()
                };

                isSuccess = _taggedItemsRepo.Update(updatedDto);
            }
        }
        else
        {
            folderDto = new FolderDto { FullPath = item.FullPath, Tags = tags.ToList() };
            isSuccess = _taggedItemsRepo.Insert(folderDto);
        }

        return !isSuccess
            ? null
            : new TaggedItem
            {
                Tags = folderDto.Tags.Select(dto => new Tag
                    {
                        Name = dto.Name,
                        TaggedItems = null
                    })
                    .ToHashSet(),
                ItemType = default,
                UniqueIdentifier = null
            };
    }

    public TaggedItem? Untag(Folder item, string[] tagNames)
    {
        var folderDto = _taggedItemsRepo.FindOne(new FolderDto { FullPath = item.FullPath });

        if (folderDto is null) return null;

        foreach (var tagDto in folderDto.Tags.ToArray())
        {
            if (!tagNames.Contains(tagDto.Name)) continue;

            folderDto.Tags.Remove(tagDto);
        }

        var isSuccess = _taggedItemsRepo.Update(folderDto);

        return isSuccess
            ? new TaggedItem
            {
                Tags = folderDto.Tags.Select(dto => new Tag
                    {
                        Name = dto.Name,
                        TaggedItems = null
                    })
                    .ToHashSet(),
                ItemType = default,
                UniqueIdentifier = null
            }
            : null;
    }
}
