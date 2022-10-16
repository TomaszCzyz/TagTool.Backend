using LiteDB;

namespace TagTool.Backend.Repositories.Dtos;

public abstract class TaggedItemDto
{
    public int Id { get; init; }

    public abstract string UniqueKey { get; }

    [BsonRef("Tags")]
    public List<TagDto> Tags { get; init; } = new();
}
