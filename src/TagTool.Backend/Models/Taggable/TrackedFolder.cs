namespace TagTool.Backend.Models.Taggable;

public class TrackedFolder : ITaggable
{
    public int Id { get; set; }

    public string FullPath { get; set; }

    public Task<bool> Tag(string tagName) => throw new NotImplementedException();

    public Task<bool> Untag(string tagName) => throw new NotImplementedException();
}
