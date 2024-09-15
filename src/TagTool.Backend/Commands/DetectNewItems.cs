using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class DetectNewItemsRequest : ICommand<OneOf<IEnumerable<TaggableItem>>>;

[UsedImplicitly]
public class DetectNewItems : ICommandHandler<DetectNewItemsRequest, OneOf<IEnumerable<TaggableItem>>>
{
    private readonly string[] _watchedDirs = ["/home/tczyz/Downloads"];

    private readonly ILogger<DetectNewItems> _logger;
    private readonly ITagToolDbContext _dbContext;

    public DetectNewItems(ILogger<DetectNewItems> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<IEnumerable<TaggableItem>>> Handle(DetectNewItemsRequest request, CancellationToken cancellationToken)
    {
        var notTaggedItems = new List<TaggableItem>();

        foreach (var watchedDir in _watchedDirs)
        {
            try
            {
                var info = new DirectoryInfo(watchedDir);
                var items = await CheckChildren(info, cancellationToken);
                notTaggedItems.AddRange(items);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to create DirectoryInfo from path {DirPath}", watchedDir);
            }
        }

        return notTaggedItems;
    }

    private async Task<IEnumerable<TaggableItem>> CheckChildren(DirectoryInfo directoryInfo, CancellationToken cancellationToken)
    {
        var filePaths = directoryInfo.EnumerateFiles().Select(info => info.FullName).ToHashSet();
        var dirPaths = directoryInfo.EnumerateDirectories().Select(info => info.FullName).ToHashSet();

        var taggedFiles = await _dbContext.TaggableFiles
            .Where(file => filePaths.Contains(file.Path))
            .Select(file => file.Path)
            .ToArrayAsync(cancellationToken);

        var taggedDirs = await _dbContext.TaggableFolders
            .Where(dir => dirPaths.Contains(dir.Path))
            .Select(dir => dir.Path)
            .ToArrayAsync(cancellationToken);

        var notTaggedFiles = filePaths
            .Select(path => new TaggableFile { Path = path })
            .ExceptBy(taggedFiles, file => file.Path)
            .Cast<TaggableItem>();

        var notTaggedDirs = dirPaths
            .Select(path => new TaggableFolder { Path = path })
            .ExceptBy(taggedDirs, dir => dir.Path)
            .Cast<TaggableItem>();

        return notTaggedDirs.Concat(notTaggedFiles);
    }
}
