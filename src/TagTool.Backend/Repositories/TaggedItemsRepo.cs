using LiteDB;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Repositories;

public interface ITaggedItemsRepo
{
    FileDto? FindFile(File file);

    bool Insert(FileDto taggedFile);

    bool Update(FileDto taggedFile);
}

public class TaggedItemsRepo : ITaggedItemsRepo
{
    private readonly ILogger<TaggedItemsRepo> _logger;

    public TaggedItemsRepo(ILogger<TaggedItemsRepo> logger)
    {
        _logger = logger;
    }

    public FileDto? FindFile(File file)
    {
        using var taggedItems = new TaggedItemsCollection();

        var fileDto = (FileDto?)taggedItems.Collection
            .Include(fileDto => fileDto.Tags)
            .FindOne(
                $"$._type = 'TagTool.Backend.Repositories.FileDto, TagTool.Backend' AND $.FullPath = '{file.FullPath.Replace(@"\", @"\\")}'");

        return fileDto;
    }

    public bool Insert(FileDto fileDto)
    {
        using var taggedItems = new TaggedItemsCollection();

        var bsonValue = taggedItems.Collection.Insert(fileDto);
        var isSuccess = bsonValue.Type != BsonType.Null;

        if (isSuccess)
        {
            _logger.LogInformation("Inserted a new file {@FileDto}", fileDto);
        }
        else
        {
            _logger.LogWarning("Not able to insert a new file {@FileDto}", fileDto);
        }

        return isSuccess;
    }

    public bool Update(FileDto fileDto)
    {
        using var taggedItems = new TaggedItemsCollection();

        var isUpdated = taggedItems.Collection.Update(fileDto);
        if (isUpdated)
        {
            _logger.LogInformation("Updated the tagged item {@TaggedItem}", fileDto);
        }
        else
        {
            _logger.LogWarning("Unable to update tagged item {@TaggedItem}", fileDto);
        }

        return isUpdated;
    }
}
