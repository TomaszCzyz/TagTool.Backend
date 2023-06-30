using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public enum QuerySegmentState
{
    Exclude = 0,
    Include = 1,
    MustBePresent = 2
}

public class TagQuerySegment
{
    public QuerySegmentState State { get; init; } = QuerySegmentState.Include;

    public required TagBase Tag { get; init; }
}
