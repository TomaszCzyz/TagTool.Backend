using LiteDB;
using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public class TagsCollection : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<TagDto> Collection { get; }

    public TagsCollection()
    {
        _db = new LiteDatabase(Constants.Constants.DbPath);
        Collection = _db.GetCollection<TagDto>("Tags");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

public class TaggedItemsCollection : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<TaggedItemDto> Collection { get; }

    public TaggedItemsCollection()
    {
        _db = new LiteDatabase(Constants.Constants.DbPath);
        Collection = _db.GetCollection<TaggedItemDto>("TaggedItems");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
