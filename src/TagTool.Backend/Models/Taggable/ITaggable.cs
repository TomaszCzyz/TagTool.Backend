namespace TagTool.Backend.Models.Taggable;

public interface ITaggable
{
    Task<bool> Tag(string tagName);

    Task<bool> Untag(string tagName);
}
