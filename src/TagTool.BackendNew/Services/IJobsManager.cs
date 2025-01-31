using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services;

public interface IJob
{
    Task Execute(CancellationToken cancellationToken);
}

public interface IJobsManager
{
}

public class TagFilesInFolderJob : IJob
{
    private readonly ILogger<TagFilesInFolderJob> _logger;
    private readonly ITagToolDbContext _dbContext;

    public required string Path { get; init; }

    public required int[] TagIds { get; init; }

    public TagFilesInFolderJob(ILogger<TagFilesInFolderJob> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting job {JobName}", nameof(TagFilesInFolderJob));
        var tags = await _dbContext.Tags.Where(t => TagIds.Contains(t.Id)).ToArrayAsync(cancellationToken);

        if (tags.Length == 0)
        {
            _logger.LogWarning("No tags found");
            // TODO: disable job
            return;
        }

        DirectoryInfo directoryInfo;
        try
        {
            directoryInfo = new DirectoryInfo(Path);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to create DirectoryInfo for {Path}", Path);
            return;
        }

        var filePathsChunks = directoryInfo.EnumerateFiles().Select(info => info.FullName).Chunk(50);
        foreach (var filePaths in filePathsChunks)
        {
            var trackedFiles = await _dbContext.Set<TaggableFile.TaggableFile>()
                .Include(t => t.Tags)
                .Where(file => filePaths.Contains(file.Path))
                .ToArrayAsync(cancellationToken);

            var untrackedFilePaths = filePaths.Except(trackedFiles.Select(f => f.Path));

            TagTrackedFiles(trackedFiles, tags, cancellationToken);
            TagUntrackedFiles(untrackedFilePaths, tags);

            var count = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated {Count} entities with tag(s) {@Tags}", count, tags);
        }
    }

    private static void TagTrackedFiles(TaggableFile.TaggableFile[] trackedFiles, TagBase[] tags, CancellationToken cancellationToken)
    {
        foreach (var trackedFile in trackedFiles)
        {
            trackedFile.Tags.UnionWith(tags);
        }
    }

    private void TagUntrackedFiles(IEnumerable<string> untrackedFilePaths, TagBase[] tags)
    {
        foreach (var untrackedFilePath in untrackedFilePaths)
        {
            var taggableFile = new TaggableFile.TaggableFile
            {
                Id = Guid.NewGuid(),
                Path = untrackedFilePath,
                Tags = tags.ToHashSet(),
            };
            _dbContext.Set<TaggableFile.TaggableFile>().Add(taggableFile);
        }
    }
}
