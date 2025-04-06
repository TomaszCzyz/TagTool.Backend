namespace TagTool.BackendNew.Entities;

public interface ITimestamped
{
    DateTime CreatedOnUtc { get; }

    DateTime? ModifiedOnUtc { get; }
}
