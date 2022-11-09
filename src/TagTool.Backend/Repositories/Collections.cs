using LiteDB;
using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public class Tags : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<TagDto> Collection { get; }

    public Tags()
    {
        _db = new LiteDatabase(Constants.Constants.DbPath);
        Collection = _db.GetCollection<TagDto>("Tags");
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class TaggedItems : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<TaggedItemDto> Collection { get; }

    public TaggedItems()
    {
        _db = new LiteDatabase(Constants.Constants.DbPath);
        Collection = _db.GetCollection<TaggedItemDto>("TaggedItems");
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
