using LiteDB;
using TagTool.Backend.Models.Taggable;
using File = TagTool.Backend.Models.Taggable.File;

namespace TagTool.Backend.Repositories;

public interface ITaggedItemsRepo
{
    FileDto? FindFile(File file);

    FolderDto? FindFolder(Folder folder);

    bool Insert(TaggedItemDto folderDto);

    bool Update(TaggedItemDto taggedItemDto);
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
            .FindOne("$._type = 'TagTool.Backend.Repositories.FileDto, TagTool.Backend'" +
                     $"AND $.FullPath = '{file.FullPath.Replace(@"\", @"\\")}'");

        return fileDto;
    }

    public FolderDto? FindFolder(Folder folder)
    {
        using var taggedItems = new TaggedItemsCollection();

        var folderDto = (FolderDto?)taggedItems.Collection
            .Include(fileDto => fileDto.Tags)
            .FindOne("$._type = 'TagTool.Backend.Repositories.FolderDto, TagTool.Backend'" +
                     $"AND $.FullPath = '{folder.FullPath.Replace(@"\", @"\\")}'");

        return folderDto;
    }

    public bool Insert(TaggedItemDto taggedItemDto)
    {
        using var taggedItems = new TaggedItemsCollection();

        var bsonValue = taggedItems.Collection.Insert(taggedItemDto);
        var isSuccess = bsonValue.Type != BsonType.Null;

        if (isSuccess)
        {
            _logger.LogInformation("Inserted a new tagged item {@TaggedItem}", taggedItemDto);
        }
        else
        {
            _logger.LogWarning("Not able to insert a new tagged item {@TaggedItem}", taggedItemDto);
        }

        return isSuccess;
    }

    public bool Update(TaggedItemDto taggedItemDto)
    {
        using var taggedItems = new TaggedItemsCollection();

        var isUpdated = taggedItems.Collection.Update(taggedItemDto);
        if (isUpdated)
        {
            _logger.LogInformation("Updated the tagged item {@TaggedItem}", taggedItemDto);
        }
        else
        {
            _logger.LogWarning("Unable to update tagged item {@TaggedItem}", taggedItemDto);
        }

        return isUpdated;
    }
}
