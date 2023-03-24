using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public interface ITagsRepo
{
    bool Exists(string tagName);

    TagDto Insert(string tagName);

    ISet<TagDto> AddIfNotExist(IEnumerable<string> tagNames);

    bool DeleteTag(string tagName);

    // todo: it should return bool depending if deletion was successful
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

    public bool Exists(string tagName)
    {
        var tags = new Tags();
        return tags.Collection.Exists(dto => dto.Name == tagName);
    }

    public TagDto Insert(string tagName)
    {
        var tagDto = new TagDto { Name = tagName };

        var tags = new Tags();
        tags.Collection.Insert(tagDto);

        return tagDto;
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

    public bool DeleteTag(string tagName)
    {
        var tags = new Tags();

        var existingTag = tags.Collection.Find(tag => tag.Name == tagName).Single();

        // todo: is it removing tag also from taggedItem?
        return tags.Collection.Delete(existingTag.Id);
    }

    public void DeleteTags(IEnumerable<string> tagNames)
    {
        // todo: is it removing tag also from taggedItem?
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
