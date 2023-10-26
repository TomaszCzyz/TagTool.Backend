using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using Xunit;
using DayTag = TagTool.Backend.Models.Tags.DayTag;
using Enum = System.Enum;
using MonthTag = TagTool.Backend.Models.Tags.MonthTag;

namespace TagTool.Backend.Tests.Integration.Services;

public class TagServiceTests : IClassFixture<CustomWebApplicationFactory<AssemblyMarker>>
{
    private readonly CustomWebApplicationFactory<AssemblyMarker> _factory;
    private readonly ITagToolDbContext _dbContext;

    public TagServiceTests(CustomWebApplicationFactory<AssemblyMarker> factory)
    {
        _factory = factory;
        var serviceScope = _factory.Services.CreateScope();
        _dbContext = serviceScope.ServiceProvider.GetRequiredService<ITagToolDbContext>();
    }

    [Fact]
    public void CheckIfBuildInTagModelsWereAdded()
    {
        var dayTags = Enum.GetValues<DayOfWeek>().Select(day => new DayTag { Id = 1000 + (int)day, DayOfWeek = day });
        var monthTags = Enumerable.Range(1, 12).Select(month => new MonthTag { Id = 2000 + month, Month = month });
        var itemTypeTags = new[]
        {
            new ItemTypeTag { Id = 3002, Type = typeof(TaggableFile) }, new ItemTypeTag { Id = 3003, Type = typeof(TaggableFolder) }
        };

        _dbContext.Tags.OfType<DayTag>().Should().BeEquivalentTo(dayTags);
        _dbContext.Tags.OfType<MonthTag>().Should().BeEquivalentTo(monthTags);
        _dbContext.Tags.OfType<ItemTypeTag>().Should().BeEquivalentTo(itemTypeTags);
    }

    [Fact]
    public async Task CreateTag_ValidRequest_TagCreated()
    {
        // Arrange
        var tagName = "IntegrationTest";
        var client = new TagService.TagServiceClient(_factory.Channel);

        // Act
        var response = await client.CreateTagAsync(new CreateTagRequest { Tag = Any.Pack(new NormalTag { Name = tagName }) });

        // Assert
        response.Tag.Is(NormalTag.Descriptor);
        response.Tag.Unpack<NormalTag>().Name.Should().Be(tagName);
        _dbContext.Tags.OfType<TextTag>().Should().Contain(tag => tag.Text == tagName);
    }
}
