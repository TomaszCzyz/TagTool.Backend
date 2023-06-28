using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

public interface IImplicitTagsProvider
{
    IEnumerable<TagBase> GetImplicitTags(TaggableItem taggableItem);
}

public class ImplicitTagsProvider : IImplicitTagsProvider
{
    private readonly Dictionary<string, TagBase[]> _extensionsToTagsMap
        = new()
        {
            { ".txt", new TagBase[] { new NormalTag { Name = "TXT" }, new NormalTag { Name = "Text" } } },
            { ".jpg", new TagBase[] { new NormalTag { Name = "JPG" }, new NormalTag { Name = "Photo" }, new NormalTag { Name = "Graphics" } } },
            { ".png", new TagBase[] { new NormalTag { Name = "PNG" }, new NormalTag { Name = "Graphics" } } },
            { ".pdf", new TagBase[] { new NormalTag { Name = "PDF" }, new NormalTag { Name = "Document" } } },
            { ".svg", new TagBase[] { new NormalTag { Name = "SVG" }, new NormalTag { Name = "Graphics" } } },
            { ".docx", new TagBase[] { new NormalTag { Name = "DOCX" }, new NormalTag { Name = "Document" }, new NormalTag { Name = "Text" } } },
            { ".doc", new TagBase[] { new NormalTag { Name = "DOC" }, new NormalTag { Name = "Document" }, new NormalTag { Name = "Text" } } }
        };

    private readonly TagToolDbContext _dbContext;

    public ImplicitTagsProvider(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;

        var dictTagNames = _extensionsToTagsMap.Values
            .SelectMany(bases => bases)
            .Select(@base => @base.FormattedName)
            .ToArray();

        // Ensure that returned tags exist in db.
        var existingTags = _dbContext.Tags
            .Where(tagBase => dictTagNames.Contains(tagBase.FormattedName))
            .Select(@base => @base.FormattedName)
            .ToArray();

        var newTags = _extensionsToTagsMap.Values
            .SelectMany(bases => bases)
            .ExceptBy(existingTags, s => s.FormattedName)
            .ToArray();

        _dbContext.Tags.AddRange(newTags);
        _dbContext.SaveChanges();
    }

    public IEnumerable<TagBase> GetImplicitTags(TaggableItem taggableItem)
    {
        var implicitTags = new List<TagBase>();

        switch (taggableItem)
        {
            case TaggableFile file:
                implicitTags.Add(new FileTypeTag());

                var ext = Path.GetExtension(file.Path);
                if (_extensionsToTagsMap.TryGetValue(ext, out var tags))
                {
                    implicitTags.AddRange(tags);
                }

                break;
            case TaggableFolder:
                implicitTags.Add(new FolderTypeTag());
                break;
        }

        return _dbContext.Tags.Where(tagBas => implicitTags.Select(@base => @base.FormattedName).Contains(tagBas.FormattedName));
    }
}
