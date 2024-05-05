using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Tests.Integration.Utilities;
using Xunit;
using DayTag = TagTool.Backend.Models.Tags.DayTag;
using Enum = System.Enum;
using MonthTag = TagTool.Backend.Models.Tags.MonthTag;

namespace TagTool.Backend.Tests.Integration.Services;

public class TagServiceTests : IClassFixture<CustomWebApplicationFactory<AssemblyMarker>>
{
    private const string TestTagName = "IntegrationTestTag";
    private readonly CustomWebApplicationFactory<AssemblyMarker> _factory;
    private readonly ITagToolDbContext _dbContext;
    private readonly ITagMapper _tagMapper = TagMapperHelper.InitializeWithKnownMappers();

    private TagService.TagServiceClient Client => new(_factory.Channel);

    public TagServiceTests(CustomWebApplicationFactory<AssemblyMarker> factory)
    {
        _factory = factory;
        // _factory.LoggedMessage += (level, name, id, message, exception) => testOutputHelper.WriteLine(message);
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
        var testNormalTag = new NormalTag { Name = TestTagName };

        // Act
        var response = await Client.CreateTagAsync(new CreateTagRequest { Tag = Any.Pack(testNormalTag) });

        // Assert
        response.ResultCase.Should().Be(CreateTagReply.ResultOneofCase.Tag);
        response.Tag.Is(NormalTag.Descriptor).Should().BeTrue();
        response.Tag.Unpack<NormalTag>().Name.Should().Be(testNormalTag.Name);
        _dbContext.Tags.OfType<TextTag>().Should().Contain(tag => tag.Text == testNormalTag.Name);

        // cleanup
        _dbContext.Tags.Remove(_dbContext.Tags.OfType<TextTag>().Single(t => t.Text == testNormalTag.Name));
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateTag_InvalidRequest_EmptyTag_Throws()
    {
        // Arrange
        // Act
        var act = async () => await Client.CreateTagAsync(new CreateTagRequest());

        // Assert
        await act.Should().ThrowAsync<RpcException>().WithMessage("*Tag*");
    }

    [Fact]
    public async Task CanCreateTag_ValidRequests_ReturnsCorrectReplies()
    {
        // Arrange
        var textTag = new TextTag { Text = "TakenName" };
        _dbContext.Tags.Add(textTag);
        _dbContext.SaveChanges();

        // Act
        var streamingCall = Client.CanCreateTag();
        await streamingCall.RequestStream.WriteAsync(new CanCreateTagRequest { TagName = "TakenName" });
        await streamingCall.RequestStream.WriteAsync(new CanCreateTagRequest { TagName = "Monday" });
        await streamingCall.RequestStream.WriteAsync(new CanCreateTagRequest());
        await streamingCall.RequestStream.WriteAsync(new CanCreateTagRequest { TagName = "NotTakenName" });
        await streamingCall.RequestStream.CompleteAsync();

        // Assert
        var replies = await streamingCall.ResponseStream.ReadAllAsync().ToArrayAsync();

        replies.Should().HaveCount(4);
        replies[0].ResultCase.Should().Be(CanCreateTagReply.ResultOneofCase.Error);
        replies[0].Error.Should().NotBeNull();
        replies[1].ResultCase.Should().Be(CanCreateTagReply.ResultOneofCase.Error);
        replies[1].Error.Should().NotBeNull();
        replies[2].ResultCase.Should().Be(CanCreateTagReply.ResultOneofCase.Error);
        replies[2].Error.Should().NotBeNull();
        replies[3].ResultCase.Should().Be(CanCreateTagReply.ResultOneofCase.None);

        _dbContext.Tags.Remove(textTag);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task DeleteTag_TagExistsAndIsUnused_TagDeleted()
    {
        // Arrange
        var testNormalTag = new NormalTag { Name = TestTagName };
        var textTag = new TextTag { Text = TestTagName };

        _dbContext.Tags.Add(textTag);
        _dbContext.SaveChanges();

        // Act
        var response = await Client.DeleteTagAsync(new DeleteTagRequest { Tag = Any.Pack(testNormalTag) });

        // Assert
        response.Tag.Is(NormalTag.Descriptor).Should().BeTrue();
        response.Tag.Unpack<NormalTag>().Name.Should().Be(testNormalTag.Name);
        _dbContext.Tags.OfType<TextTag>().Should().NotContain(tag => tag.Text == testNormalTag.Name);
    }

    [Fact]
    public async Task DeleteTag_TagExistsButIsUsed_ReturnError()
    {
        // Arrange
        var testNormalTag = new NormalTag { Name = TestTagName };
        var textTag = new TextTag { Text = TestTagName };
        var taggableItem = new TaggableFile { Path = "TestPath", Tags = [textTag] };

        _dbContext.TaggedItems.Add(taggableItem);
        _dbContext.SaveChanges();

        // Act
        var response = await Client.DeleteTagAsync(new DeleteTagRequest { Tag = Any.Pack(testNormalTag) });

        // Assert
        response.ResultCase.Should().Be(DeleteTagReply.ResultOneofCase.ErrorMessage);
        response.ErrorMessage.Should()
            .Be($"Tag {textTag.FormattedName} is in use and it was not deleted. If you want to delete this tag use DeleteUsedToo flag.");

        _dbContext.Tags.OfType<TextTag>().Should().Contain(tag => tag.Text == testNormalTag.Name);

        // cleanup
        _dbContext.TaggedItems.Remove(taggableItem);
        _dbContext.Tags.Remove(textTag);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task DeleteTag_TagExistsButIsUsed_WithFlag_DeletesTagAndUntagsItems()
    {
        // Arrange
        var testNormalTag = new NormalTag { Name = TestTagName };
        var textTag = new TextTag { Text = TestTagName };
        var taggableItem = new TaggableFile { Path = "TestPath", Tags = [textTag] };

        _dbContext.TaggedItems.Add(taggableItem);
        _dbContext.SaveChanges();

        // Act
        var response = await Client.DeleteTagAsync(new DeleteTagRequest { Tag = Any.Pack(testNormalTag), DeleteUsedToo = true });

        // Assert
        response.ResultCase.Should().Be(DeleteTagReply.ResultOneofCase.Tag);
        response.Tag.Is(NormalTag.Descriptor).Should().BeTrue();
        response.Tag.Unpack<NormalTag>().Name.Should().Be(testNormalTag.Name);
        _dbContext.Tags.OfType<TextTag>().Should().NotContain(tag => tag.Text == testNormalTag.Name);

        // Force refreshing entity on next load to get updated tags list
        _dbContext.Entry(taggableItem).State = EntityState.Detached;

        var item = _dbContext.TaggedItems.Include(item => item.Tags).Single(item => item.Id == taggableItem.Id);
        item.Should().NotBeNull();
        item.Tags.Should().NotContain(textTag);

        // cleanup
        _dbContext.TaggedItems.Remove(item);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task AddSynonym_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        Database.ReinitializeDbForTests(_dbContext);

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
        Database.ReinitializeDbForTests(_dbContext);

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
