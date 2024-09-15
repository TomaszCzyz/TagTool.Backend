using System.Text.Json;
using Microsoft.Extensions.Options;
using TagTool.Backend.Models.Options;

namespace TagTool.Backend.Services;

/// <summary>
///     This class subscribes to changes in the <see cref="UserConfiguration"/> class
///     and updates the configuration file.
/// </summary>
public class UserConfigurationWatcher
{
    private readonly ILogger<UserConfigurationWatcher> _logger;
    private readonly AppOptions _appOptions;
    private readonly UserConfiguration _userConfiguration;

    public UserConfigurationWatcher(
        ILogger<UserConfigurationWatcher> logger,
        IOptions<AppOptions> appOptions,
        UserConfiguration userConfiguration)
    {
        _logger = logger;
        _appOptions = appOptions.Value;
        _userConfiguration = userConfiguration;

        _userConfiguration.PropertyChanged += (sender, _) =>
        {
            _logger.LogInformation("User configuration changed, {SenderType}", sender?.GetType());
            UpdateConfiguration();
        };
    }

    private void UpdateConfiguration()
    {
        _logger.LogInformation("Updating configuration");

        var config = JsonSerializer.Serialize(_userConfiguration);
        File.WriteAllText(_appOptions.UserConfigFilePath, config);
    }
}
