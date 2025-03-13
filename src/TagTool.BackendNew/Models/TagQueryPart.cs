using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Models;

public enum QueryPartState
{
    Exclude = 0,
    Include = 1,
    MustBePresent = 2
}

public class TagQueryPart
{
    public int Id { get; init; }

    public QueryPartState State { get; init; } = QueryPartState.Include;

    public required TagBase Tag { get; init; }
}

public class TagQueryParam
{
    public QueryPartState State { get; init; } = QueryPartState.Include;

    public required int TagId { get; init; }
}

