using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Models;

public class TagQueryParam
{
    public QueryPartState State { get; init; } = QueryPartState.Include;

    public required int TagId { get; init; }
}
