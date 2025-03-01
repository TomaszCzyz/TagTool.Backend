﻿using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Models;

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

public class TagQuery
{
    public required TagQuerySegment[] QuerySegments { get; init; }
}
