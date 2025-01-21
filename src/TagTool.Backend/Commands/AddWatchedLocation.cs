using System.Security;
using Hangfire.Annotations;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

using Response = OneOf<Success, InsufficientPermissions, PathTooLong, DirectoryNotExists>;

public struct InsufficientPermissions;

public struct PathTooLong;

public struct DirectoryNotExists;

public class AddWatchedLocationRequest : ICommand<Response>
{
    public required string Path { get; init; }
}

[UsedImplicitly]
public class AddWatchedLocationRequestHandler : ICommandHandler<AddWatchedLocationRequest, Response>
{
    private readonly ILogger<AddWatchedLocationRequestHandler> _logger;
    private readonly UserConfiguration _userConfiguration;

    public AddWatchedLocationRequestHandler(ILogger<AddWatchedLocationRequestHandler> logger, UserConfiguration userConfiguration)
    {
        _logger = logger;
        _userConfiguration = userConfiguration;
    }

    public Task<Response> Handle(AddWatchedLocationRequest request, CancellationToken cancellationToken)
    {
        DirectoryInfo directoryInfo;
        try
        {
            directoryInfo = new DirectoryInfo(request.Path);
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

        if (!directoryInfo.Exists)
        {
            return Task.FromResult((Response)new DirectoryNotExists());
        }

        _userConfiguration.WatchedLocations.Add(Path.GetFullPath(directoryInfo.FullName));

        return Task.FromResult<Response>(new Success());
    }
}
