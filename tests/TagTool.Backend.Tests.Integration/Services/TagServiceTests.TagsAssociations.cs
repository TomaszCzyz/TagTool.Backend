using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TagTool.Backend.DbContext;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Tests.Integration.Utilities;
using Xunit;

namespace TagTool.Backend.Tests.Integration.Services;

public class TagServiceTestsTagsAssociations : IClassFixture<CustomWebApplicationFactory<AssemblyMarker>>
{
    private readonly CustomWebApplicationFactory<AssemblyMarker> _factory;
    private readonly ITagToolDbContext _dbContext;
    private readonly ITagMapper _tagMapper = TagMapperHelper.InitializeWithKnownMappers();

    private TagService.TagServiceClient Client => new(_factory.Channel);

    public TagServiceTestsTagsAssociations(CustomWebApplicationFactory<AssemblyMarker> factory)
    {
        _factory = factory;
        var serviceScope = _factory.Services.CreateScope();
        _dbContext = serviceScope.ServiceProvider.GetRequiredService<ITagToolDbContext>();
    }

    [Fact]
    public async Task AddSynonym_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        Database.ClearTagsAssociations(_dbContext);

        var tag1 = new TextTag { Text = "TestTag" };
        var tag2 = new TextTag { Text = "TestTag2" };
        var groupName = "TestGroupName";

        // Act
        var reply1 = await Client.AddSynonymAsync(new AddSynonymRequest { Tag = _tagMapper.MapToDto(tag1), GroupName = groupName });
        var reply2 = await Client.AddSynonymAsync(new AddSynonymRequest { Tag = _tagMapper.MapToDto(tag2), GroupName = groupName });

        // Assert
        reply1.ResultCase.Should().Be(AddSynonymReply.ResultOneofCase.SuccessMessage);
        reply1.SuccessMessage.Should().NotBeNull();
        reply2.ResultCase.Should().Be(AddSynonymReply.ResultOneofCase.SuccessMessage);
        reply2.SuccessMessage.Should().NotBeNull();

        var group = _dbContext.TagSynonymsGroups.Include(g => g.Synonyms).Single(g => g.Name == groupName);
        group.Should().NotBeNull();
        group.Synonyms.Should().NotBeEmpty().And.HaveCount(2);
        group.Synonyms.Select(s => s.FormattedName).Should().BeEquivalentTo(tag1.FormattedName, tag2.FormattedName);
    }

    [Fact]
    public async Task RemoveSynonym_GroupWithMoreThanOneSynonym_RemovesGivenSynonym()
    {
        // Arrange
        Database.ClearTagsAssociations(_dbContext);

        var tag1 = new TextTag { Text = "TestTag" };
        var tag2 = new TextTag { Text = "TestTag2" };
        var groupName = "TestGroupName";
        var tagSynonymsGroup = new TagSynonymsGroup { Name = groupName, Synonyms = new List<TagBase> { tag1, tag2 } };
        _dbContext.TagSynonymsGroups.Add(tagSynonymsGroup);
        _dbContext.SaveChanges();

        // Act
        var reply = await Client.RemoveSynonymAsync(new RemoveSynonymRequest { Tag = _tagMapper.MapToDto(tag2), GroupName = groupName });

        // Assert
        reply.ResultCase.Should().Be(RemoveSynonymReply.ResultOneofCase.SuccessMessage);
        reply.SuccessMessage.Should().NotBeNull();

        _dbContext.ChangeTracker.Clear();
        var group = _dbContext.TagSynonymsGroups.Include(g => g.Synonyms).Single(g => g.Name == groupName);
        group.Should().NotBeNull();
        group.Synonyms.Should().NotBeEmpty().And.HaveCount(1);
        group.Synonyms.Select(s => s.FormattedName).Should().BeEquivalentTo(tag1.FormattedName);
    }
}
