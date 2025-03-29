namespace TagTool.BackendNew.Contracts;

public interface ITag
{
    string Text { get; }

    ICollection<TaggableItem> TaggedItems { get; set; }
}
