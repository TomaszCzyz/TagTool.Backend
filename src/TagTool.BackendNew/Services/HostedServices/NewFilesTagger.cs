using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services.HostedServices;

public class NewFilesTaggerPayload
{
    public string Path { get; init; } = string.Empty;
    public List<int> TagIds { get; init; } = [];
}

// ensure ONLY one instance is running
public class NewFilesTagger : IHostedService, IDisposable
{
    private readonly ILogger<NewFilesTagger> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private ITagToolDbContext? _dbContext;
    private FileSystemWatcher? _watcher;
    private readonly NewFilesTaggerPayload _payload;

    public NewFilesTagger(ILogger<NewFilesTagger> logger, IServiceScopeFactory scopeFactory, IHostApplicationLifetime applicationLifetime)
    {
        // CancellationToken token = _appLifetime.ApplicationStopping;
        // return await DoSomethingAsync(token);
        // applicationLifetime.ApplicationStopped.Register(() => StopAsync());

        _logger = logger;
        _scopeFactory = scopeFactory;

        _payload = new NewFilesTaggerPayload
        {
            Path = "/home/tczyz/Documents/TagTool/TagToolPlayground",
            TagIds = [5]
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting hosted service {HostedServiceName}", nameof(NewFilesTagger));

        using var serviceScope = _scopeFactory.CreateScope();
        _dbContext = serviceScope.ServiceProvider.GetRequiredService<ITagToolDbContext>();

        var tags = await _dbContext.Tags
            .Where(t => _payload.TagIds.Contains(t.Id))
            .ToArrayAsync(cancellationToken);

        if (tags.Length == 0)
        {
            _logger.LogWarning("No tags found");
            // TODO: disable job
            return;
        }

        await OneTimeScan(_payload, tags, cancellationToken);
        LaunchFileWatcher(_payload);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void LaunchFileWatcher(NewFilesTaggerPayload payload)
    {
        _watcher = new FileSystemWatcher(payload.Path)
        {
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
        };

        _watcher.Created += OnCreated;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(e.FullPath))
        {
            _logger.LogDebug("Created item no longer exists or it is a directory, skipping...");
            return;
        }

        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(e.FullPath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to create FileInfo for {Path}", e.FullPath);
            return;
        }

        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
        {
            _logger.LogDebug("Created file is hidden, skipping...");
        }

        using var serviceScope = _scopeFactory.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ITagToolDbContext>();

        var tags = dbContext.Tags
            .Where(t => _payload.TagIds.Contains(t.Id))
            .ToList();

        if (tags.Count == 0)
        {
            _logger.LogWarning("Tags not found");
            // TODO: disable job?
            return;
        }

        _logger.LogInformation("New file {FullPath} detected, tagging with tags {@Tags}", e.FullPath, tags);
        var trackedFile = dbContext.Set<TaggableFile.TaggableFile>()
            .Include(t => t.Tags)
            .FirstOrDefault(file => e.FullPath.Contains(file.Path));


        if (trackedFile is not null)
        {
            trackedFile.Tags.UnionWith(tags);
        }
        else
        {
            var taggableFile = new TaggableFile.TaggableFile
            {
                Id = Guid.NewGuid(),
                Path = e.FullPath,
                Tags = tags.ToHashSet(),
            };
            dbContext.Set<TaggableFile.TaggableFile>().Add(taggableFile);
        }

        dbContext.SaveChanges();
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        using var serviceScope = _scopeFactory.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ITagToolDbContext>();

        var oldTrackedFile = dbContext.Set<TaggableFile.TaggableFile>()
            .FirstOrDefault(file => e.OldFullPath.Contains(file.Path));

        var trackedFile = dbContext.Set<TaggableFile.TaggableFile>()
            .FirstOrDefault(file => e.FullPath.Contains(file.Path));

        _logger.LogDebug("File rename detected");
        _logger.LogDebug("Old path: {OldPath}, new path: {NewPath}", e.OldFullPath, e.FullPath);

        // TODO: revisit this logic
        switch ((oldTrackedFile, trackedFile))
        {
            case (null, null):
                _logger.LogDebug("File is untracked, do nothing...");
                break;
            case (null, not null):
                _logger.LogInformation("File rename detected, but file with updated name is already tracked");
                break;
            case (not null, null):
                dbContext.Set<TaggableFile.TaggableFile>().Remove(oldTrackedFile);
                dbContext.Set<TaggableFile.TaggableFile>().Add(new TaggableFile.TaggableFile
                {
                    Id = Guid.CreateVersion7(),
                    Path = e.FullPath,
                    Tags = oldTrackedFile.Tags.ToHashSet()
                });
                break;
            case (not null, not null):
                _logger.LogInformation("File rename detected, updating info in db");
                dbContext.Set<TaggableFile.TaggableFile>().Remove(oldTrackedFile);
                trackedFile.Path = e.FullPath;
                dbContext.SaveChanges();
                break;
        }

        dbContext.SaveChanges();
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogWarning(e.GetException(), "Exception in FileSystemWatcher");
    }

    private async Task OneTimeScan(NewFilesTaggerPayload payload, TagBase[] tags, CancellationToken cancellationToken)
    {
        Debug.Assert(_dbContext != null, nameof(_dbContext) + " != null");

        DirectoryInfo directoryInfo;
        try
        {
            directoryInfo = new DirectoryInfo(payload.Path);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to create DirectoryInfo for {Path}", payload.Path);
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
        Debug.Assert(_dbContext != null, nameof(_dbContext) + " != null");

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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _watcher?.Dispose();
    }
}
