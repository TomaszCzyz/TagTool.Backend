using LiteDB;
using TagTool.Backend.Models;

namespace TagTool.Backend.Repositories;

public interface IConnectionsFactory
{
    LiteDatabase Create();
}

public class ConnectionsFactory : IConnectionsFactory
{
    private readonly string _connectionString;

    public ConnectionsFactory()
    {
        // Open database (or create if doesn't exist)
        _connectionString = Constants.Constants.DbPath;
        using var db = new LiteDatabase(_connectionString);

        db.UtcDate = true;
        var taggedItems = db.GetCollection<TaggableItemDto>("TaggedItems");
        var tags = db.GetCollection<Tag>("Tags");

        taggedItems.EnsureIndex(x => x.Id, true);
        tags.EnsureIndex(tag => tag.Name);
    }

    public LiteDatabase Create() => new(_connectionString);
}
