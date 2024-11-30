using System.Security;
using Hangfire.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

using Response = OneOf<Success, InsufficientPermissions, PathTooLong, DirectoryNotExists, NotFound>;

public class DeleteWatchedLocationRequest : ICommand<Response>
{
    public required string Path { get; init; }
}

[UsedImplicitly]
public class DeleteWatchedLocationRequestHandler : ICommandHandler<DeleteWatchedLocationRequest, Response>
{
    private readonly ILogger<DeleteWatchedLocationRequestHandler> _logger;
    private readonly UserConfiguration _userConfiguration;

    public DeleteWatchedLocationRequestHandler(ILogger<DeleteWatchedLocationRequestHandler> logger, UserConfiguration userConfiguration)
    {
        _logger = logger;
        _userConfiguration = userConfiguration;
    }

    public Task<Response> Handle(DeleteWatchedLocationRequest request, CancellationToken cancellationToken)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(request.Path);
        }
        catch (SecurityException ex)
        {
            _logger.LogDebug(ex, "Cannot create/access a path {Path}, because of an exception", request.Path);
            return Task.FromResult((Response)new InsufficientPermissions());
        }
        catch (PathTooLongException)
        {
            return Task.FromResult((Response)new PathTooLong());
        }

        if (!_userConfiguration.WatchedLocations.Contains(fullPath))
        {
            _logger.LogWarning("WatchedLocation {FullPath} not found in the user configuration", fullPath);
            return Task.FromResult((Response)new NotFound());
        }

        _userConfiguration.WatchedLocations.Remove(fullPath);
        return Task.FromResult<Response>(new Success());
    }
}
