using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.DomainTypes;
using Xunit;

namespace TagTool.Backend.Tests.Integration.Services;

public class TagServiceTests : IClassFixture<CustomWebApplicationFactory<AssemblyMarker>>
{
    private readonly CustomWebApplicationFactory<AssemblyMarker> _factory;

    public TagServiceTests(CustomWebApplicationFactory<AssemblyMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InvokeSomeTests()
    {
        // Arrange
        var client = new TagService.TagServiceClient(_factory.Channel);

        // Act
        var response = await client.CreateTagAsync(new CreateTagRequest { Tag = Any.Pack(new NormalTag { Name = "IntegrationTest" }) });

        // Assert
        Assert.True(true);
    }
}
