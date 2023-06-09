namespace TagTool.Backend.Models;

public class TagQuerySegment
{
    public required bool Include { get; init; }

    public required bool MustBePresent { get; init; }

    public required TagBase Tag { get; init; }
}
