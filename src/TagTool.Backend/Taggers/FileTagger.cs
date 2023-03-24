using TagTool.Backend.Models;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Repositories;
using TagTool.Backend.Repositories.Dtos;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Taggers;

public class FileTagger : ITagger<File>
{
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;

    public FileTagger(ITagsRepo tagsRepo, ITaggedItemsRepo taggedItemsRepo)
    {
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
    }

    public TaggedItem<File>? Tag(File item, string[] tagNames)
    {
        var tags = _tagsRepo.AddIfNotExist(tagNames);
        var fileDto = _taggedItemsRepo.FindOne(new FileDto { FullPath = item.FullPath });

        var isSuccess = false;

        if (fileDto is not null)
        {
            var newTags = tags.Except(fileDto.Tags, TagDto.NameComparer).ToArray();
            if (newTags.Length != 0)
            {
                var updatedDto = new FileDto
                {
                    Id = fileDto.Id,
                    FullPath = fileDto.FullPath,
                    Tags = fileDto.Tags.Concat(newTags).ToList()
                };

                isSuccess = _taggedItemsRepo.Update(updatedDto);
            }
        }
        else
        {
            fileDto = new FileDto { FullPath = item.FullPath, Tags = tags.ToList() };
            isSuccess = _taggedItemsRepo.Insert(fileDto);
        }

        return isSuccess
            ? new TaggedItem<File> { Item = item, Tags = fileDto.Tags.Select(dto => new Tag { Name = dto.Name }).ToHashSet() }
            : null;
    }

    public TaggedItem<File>? Untag(File item, string[] tagNames)
    {
        var fileDto = _taggedItemsRepo.FindOne(new FileDto { FullPath = item.FullPath });

        if (fileDto is null) return null;

        foreach (var tagDto in fileDto.Tags.ToArray())
        {
            if (!tagNames.Contains(tagDto.Name)) continue;

            fileDto.Tags.Remove(tagDto);
        }

        var isSuccess = _taggedItemsRepo.Update(fileDto);

        return isSuccess
            ? new TaggedItem<File> { Item = item, Tags = fileDto.Tags.Select(dto => new Tag { Name = dto.Name }).ToHashSet() }
            : null;
    }
}
