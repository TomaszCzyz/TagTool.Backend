using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

using Response = OneOf<IEnumerable<TaggableItem>, NoWatchedLocations>;

public struct NoWatchedLocations;

public class DetectNewItemsRequest : ICommand<Response>;

[UsedImplicitly]
public class DetectNewItems : ICommandHandler<DetectNewItemsRequest, Response>
{
    private readonly ILogger<DetectNewItems> _logger;
    private readonly ITagToolDbContext _dbContext;
    private readonly UserConfiguration _userConfiguration;

    public DetectNewItems(ILogger<DetectNewItems> logger, ITagToolDbContext dbContext, UserConfiguration userConfiguration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userConfiguration = userConfiguration;
    }

    public async Task<Response> Handle(DetectNewItemsRequest request, CancellationToken cancellationToken)
    {
        if (_userConfiguration.WatchedLocations.Count == 0)
        {
            return new NoWatchedLocations();
        }

        var notTaggedItems = new List<TaggableItem>();

        foreach (var watchedDir in _userConfiguration.WatchedLocations)
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
