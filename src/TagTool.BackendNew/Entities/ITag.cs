namespace TagTool.BackendNew.Entities;

public interface ITag
{
    string Text { get; }

    ICollection<TaggableItem> TaggedItems { get; set; }
}
