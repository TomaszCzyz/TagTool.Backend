﻿using LiteDB;
using TagTool.Backend.Models;
using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public interface ITaggedItemsRepo
{
    T? FindOne<T>(T item) where T : TaggedItemDto;

    bool Insert(TaggedItemDto taggedItemDto);

    bool Update(TaggedItemDto taggedItemDto);

    IEnumerable<TaggedItemDto> FindByTags(string[] tagNames);
}

public class TaggedItemsRepo : ITaggedItemsRepo
{
    private readonly ILogger<TaggedItemsRepo> _logger;

    public TaggedItemsRepo(ILogger<TaggedItemsRepo> logger)
    {
        _logger = logger;
    }

    public T? FindOne<T>(T item) where T : TaggedItemDto
    {
        var taggedItems = new TaggedItems();

        var taggedItem = (T?)taggedItems.Collection
            .Include(taggedItemDto => taggedItemDto.Tags)
            .FindOne($"$._type = '{typeof(T)}, TagTool.Backend' AND $.FullPath = '{item.UniqueKey.Replace(@"\", @"\\")}'");

        return taggedItem;
    }

    public bool Insert(TaggedItemDto taggedItemDto)
    {
        var taggedItems = new TaggedItems();

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
        var taggedItems = new TaggedItems();

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

    public IEnumerable<TaggedItemDto> FindByTags(string[] tagNames)
    {
        var taggedItems = new TaggedItems();

        // todo: optimize, to not download all collation
        return taggedItems.Collection
            .Include(dto => dto.Tags)
            .FindAll()
            .Where(dto => dto.Tags
                .Select(tagDto => tagDto.Name)
                .Intersect(tagNames)
                .Any());
    }
}
