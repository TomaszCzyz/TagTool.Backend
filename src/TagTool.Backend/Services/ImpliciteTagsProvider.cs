using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

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
            { ".txt", new TagBase[] { new TextTag { Text = "TXT" }, new TextTag { Text = "Text" } } },
            { ".jpg", new TagBase[] { new TextTag { Text = "JPG" }, new TextTag { Text = "Photo" }, new TextTag { Text = "Graphics" } } },
            { ".png", new TagBase[] { new TextTag { Text = "PNG" }, new TextTag { Text = "Graphics" } } },
            { ".pdf", new TagBase[] { new TextTag { Text = "PDF" }, new TextTag { Text = "Document" } } },
            { ".svg", new TagBase[] { new TextTag { Text = "SVG" }, new TextTag { Text = "Graphics" } } },
            { ".docx", new TagBase[] { new TextTag { Text = "DOCX" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" } } },
            { ".doc", new TagBase[] { new TextTag { Text = "DOC" }, new TextTag { Text = "Document" }, new TextTag { Text = "Text" } } }
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
