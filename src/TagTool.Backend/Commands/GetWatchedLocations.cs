using Hangfire.Annotations;
using OneOf;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

using Response = OneOf<string[]>;

public class GetWatchedLocationsRequest : ICommand<Response>;

[UsedImplicitly]
public class GetWatchedLocationsRequestHandler : ICommandHandler<GetWatchedLocationsRequest, Response>
{
    private readonly UserConfiguration _userConfiguration;

    public GetWatchedLocationsRequestHandler(UserConfiguration userConfiguration)
    {
        _userConfiguration = userConfiguration;
    }

    public Task<Response> Handle(GetWatchedLocationsRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Response>(_userConfiguration.WatchedLocations.ToArray());
    }
}
