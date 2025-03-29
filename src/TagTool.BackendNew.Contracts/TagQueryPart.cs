namespace TagTool.BackendNew.Contracts;

public class TagQueryPart
{
    public int Id { get; init; }

    public QueryPartState State { get; init; } = QueryPartState.Include;

    public required TagBase Tag { get; init; }
}

public enum QueryPartState
{
    Exclude = 0,
    Include = 1,
    MustBePresent = 2
}
