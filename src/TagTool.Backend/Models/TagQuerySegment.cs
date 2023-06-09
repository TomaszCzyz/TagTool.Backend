namespace TagTool.Backend.Models;

// todo: consider using the following enum in TagQuerySegment
// public enum TagAppearanceType
// {
//     Exclude,
//     Include,
//     MustBePresent
// }

public class TagQuerySegment
{
    public required bool Include { get; init; }

    public required bool MustBePresent { get; init; }

    public required TagBase Tag { get; init; }
}
