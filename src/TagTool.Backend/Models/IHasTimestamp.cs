namespace TagTool.Backend.Models;

public interface IHasTimestamps
{
    DateTime? Added { get; set; }

    DateTime? Deleted { get; set; }

    DateTime? Modified { get; set; }
}

public static class HasTimestampsExtensions
{
    public static string ToStampString(this IHasTimestamps entity)
    {
        return GetStamp("Added", entity.Added) + GetStamp("Modified", entity.Modified) + GetStamp("Deleted", entity.Deleted);

        string GetStamp(string state, DateTime? dateTime) => dateTime == null ? "" : $" {state} on: {dateTime}";
    }
}
