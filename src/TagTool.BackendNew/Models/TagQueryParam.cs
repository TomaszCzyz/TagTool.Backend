using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Models;

public class TagQueryParam
{
    public QueryPartState State { get; init; } = QueryPartState.Include;

    public required int TagId { get; init; }
}
