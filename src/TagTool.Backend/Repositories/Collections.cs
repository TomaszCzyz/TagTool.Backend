using LiteDB;
using TagTool.Backend.Repositories.Dtos;

namespace TagTool.Backend.Repositories;

public static class Lite
{
    public static readonly LiteDatabase Db = new(Constants.Constants.DbPath);
}

public class Tags
{
    public ILiteCollection<TagDto> Collection { get; }

    public Tags()
    {
        Collection = Lite.Db.GetCollection<TagDto>("Tags");
    }
}

public class TaggedItems
{
    public ILiteCollection<TaggedItemDto> Collection { get; }

    public TaggedItems()
    {
        Collection = Lite.Db.GetCollection<TaggedItemDto>("TaggedItems");
    }
}
