using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TagTool.Backend.Models.Options;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class UserConfigurationTests : IDisposable
{
    private readonly ILogger<UserConfigurationWatcher> _logger = Substitute.For<ILogger<UserConfigurationWatcher>>();

    private readonly string _userConfigFilePath = Path.Join(Constants.Constants.BasePath, "test_userConfig.json");
    private readonly OptionsWrapper<AppOptions> _appOptions;

    public UserConfigurationTests()
    {
        var options = new AppOptions { UserConfigFilePath = _userConfigFilePath };
        _appOptions = new OptionsWrapper<AppOptions>(options);
    }

    [Fact]
    public void Ctor_ConfigurationAlreadyExists_ConfigurationLoadedCorrectly()
    {
        // Arrange
        var testLocation = "test_location";

        var initialUserConfiguration = new UserConfiguration { ObservedLocations = [testLocation], };
        File.WriteAllText(_userConfigFilePath, JsonSerializer.Serialize(initialUserConfiguration));

        // TODO: implement
    }

    [Fact]
    public void Set_ObservedLocations_FileUpdated()
    {
        // Arrange
        var testLocation = "test_location";
        var testLocations2 = "test_locations2";

        var userConfiguration = new UserConfiguration();
        userConfiguration.ObservedLocations.Add(testLocation);
        var sut = new UserConfigurationWatcher(_logger, _appOptions, userConfiguration);

        // Act
        userConfiguration.ObservedLocations.Add(testLocations2);

        // Assert
        var configuration = JsonSerializer.Deserialize<UserConfiguration>(File.ReadAllText(_userConfigFilePath));

        configuration.Should().NotBeNull();
        configuration!.ObservedLocations.Should().Contain([testLocation, testLocations2]);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (File.Exists(_userConfigFilePath))
        {
            File.Delete(_userConfigFilePath);
        }
    }
}
