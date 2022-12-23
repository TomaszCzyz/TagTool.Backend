using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public interface ITagsRepo
{
    ISet<TagDto> AddIfNotExist(IEnumerable<string> tagNames);

    void DeleteTags(IEnumerable<string> tagNames);

    IEnumerable<string> GetAllTagNames();
}

public class TagsRepo : ITagsRepo
{
    private readonly ILogger<TagsRepo> _logger;

    public TagsRepo(ILogger<TagsRepo> logger)
    {
        _logger = logger;
    }

    public ISet<TagDto> AddIfNotExist(IEnumerable<string> tagNames)
    {
        var tags = new Tags();

        _logger.LogInformation("Trying to upsert tags {@TagNames} to tags collection...", tagNames);

        var existingTags = tags.Collection
            .Find(dto => tagNames.Contains(dto.Name))
            .ToList();

        _logger.LogInformation("Tags {@TagNames} already exist in the collection, skipping them...", existingTags);

        var newTags = tagNames
            .Except(existingTags.Select(dto => dto.Name))
            .Select(tagName => new TagDto { Name = tagName })
            .ToArray();

        var numInserted = tags.Collection.Insert(newTags);

        _logger.LogInformation(
            "Inserted {InsertedNum}/{ExpectedNum} ({@TagNames})", numInserted, newTags.Length, newTags.Select(dto => dto.Name));

        return existingTags.Concat(newTags).ToHashSet();
    }

    public void DeleteTags(IEnumerable<string> tagNames)
    {
        var tags = new Tags();

        var existingTags = tags.Collection
            .Find(tag => tagNames.Contains(tag.Name))
            .ToArray();

        foreach (var existingTag in existingTags)
        {
            var isDeleted = tags.Collection.Delete(existingTag.Id);
            if (!isDeleted) continue;

            _logger.LogInformation("Removed tag {@TagName} from database", existingTag.Name);
        }
    }

    public IEnumerable<string> GetAllTagNames()
    {
        var tags = new Tags();

        return tags.Collection.Query().Select(dto => dto.Name).ToArray();
    }
}
