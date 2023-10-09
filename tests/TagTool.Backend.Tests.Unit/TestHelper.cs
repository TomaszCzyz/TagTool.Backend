using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models.Tags;
using Xunit;
using Xunit.Abstractions;

namespace TagTool.Backend.Tests.Unit;

public class TestHelper
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ITagMapper _mapper;
    private readonly TagToolDbContext _dbContext;

    public TestHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var fromDto = new ITagFromDtoMapper[] { new ItemTypeTagMapper(), new TextTagMapper() };
        var toDto = new ITagToDtoMapper[] { new ItemTypeTagMapper(), new TextTagMapper() };
        _mapper = new TagMapper(fromDto, toDto);
        var dbContextOptions = new DbContextOptionsBuilder<TagToolDbContext>();
        dbContextOptions.UseSqlite($"Data Source={Constants.Constants.DbPath}");
        var mediatorMock = new Mock<IMediator>();

        _dbContext = new TagToolDbContext(mediatorMock.Object, dbContextOptions.Options);
    }

    [Fact]
    public Task CreateTag_ValidRequest_ReturnsCreatedTag()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        // var tag = new MonthRangeTag { Begin = 1, End = 10 };
        // var tag = new DayRangeTag { Begin = 1, End = 4 };
        var tag = new NormalTag { Name = "NotificationTest3" };

        var _ = tagServiceClient.CreateTag(new CreateTagRequest { Tag = Any.Pack(tag) });
        return Task.CompletedTask;
    }

    [Fact]
    public Task TagItem_ValidRequest_ReturnsTaggedItem()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        // var tag = new MonthRangeTag { Begin = 1, End = 10 };
        // var tag = new DayRangeTag { Begin = 1, End = 4 };
        // var tag = new DayTag { Day = 1 };
        var tag = new NormalTag { Name = "Cat" };

        // var fileDto = new FileDto { Path = @"C:\Users\tczyz\MyFiles\FromOec\Digital2.gif" };
        var fileDto = new FolderDto { Path = @"C:\Users\tczyz" };

        var _ = tagServiceClient.TagItem(new TagItemRequest { Tag = Any.Pack(tag), Folder = fileDto });

        return Task.CompletedTask;
    }

    [Fact]
    public Task UntagItem_ValidRequest_ReturnsTaggedItem()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        // var tag = new MonthRangeTag { Begin = 1, End = 10 };
        // var tag = new DayRangeTag { Begin = 1, End = 4 };
        var tag = new NormalTag { Name = "Cat" };

        var _ = tagServiceClient.UntagItem(
            new UntagItemRequest { Tag = Any.Pack(tag), File = new FileDto { Path = @"C:\Users\tczyz\MyFiles\FromOec\DigitalSign.gif" } });
        return Task.CompletedTask;
    }

    [Fact]
    public void AddSynonyms_ValidRequest_ReturnsSuccessMessage()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var animalTag = Any.Pack(new NormalTag { Name = "Animal" });
        var animalBaseTag = Any.Pack(new NormalTag { Name = "AnimalBase" });
        var catTag = Any.Pack(new NormalTag { Name = "Cat" });
        var cat2Tag = Any.Pack(new NormalTag { Name = "Cat2" });
        var pussyTag = Any.Pack(new NormalTag { Name = "Pussy" });
        var dogTag = Any.Pack(new NormalTag { Name = "Dog" });

        var reply1 = tagServiceClient.AddSynonym(new AddSynonymRequest { Tag = catTag, GroupName = "Cat group" });
        _testOutputHelper.WriteLine($"reply: {reply1}");
        var reply2 = tagServiceClient.AddChild(new AddChildRequest { ChildTag = catTag, ParentTag = animalTag });
        _testOutputHelper.WriteLine($"reply: {reply2}");
        var reply3 = tagServiceClient.AddChild(new AddChildRequest { ChildTag = cat2Tag, ParentTag = animalTag });
        _testOutputHelper.WriteLine($"reply: {reply3}");
        var reply4 = tagServiceClient.AddChild(new AddChildRequest { ChildTag = dogTag, ParentTag = animalTag });
        _testOutputHelper.WriteLine($"reply: {reply4}");
        var reply5 = tagServiceClient.AddChild(new AddChildRequest { ChildTag = animalTag, ParentTag = animalBaseTag });
        _testOutputHelper.WriteLine($"reply: {reply5}");
        var reply6 = tagServiceClient.AddSynonym(new AddSynonymRequest { Tag = dogTag, GroupName = "Dog group" });
        _testOutputHelper.WriteLine($"reply: {reply6}");
        var reply7 = tagServiceClient.AddSynonym(new AddSynonymRequest { Tag = pussyTag, GroupName = "Cat group" });
        _testOutputHelper.WriteLine($"reply: {reply7}");
        var reply8 = tagServiceClient.AddSynonym(new AddSynonymRequest { Tag = cat2Tag, GroupName = "Cat group" });
        _testOutputHelper.WriteLine($"reply: {reply8}");
    }

    [Fact]
    public async Task GetAllTagsAssociations_ValidRequest_Returns()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var request = new GetAllTagsAssociationsRequest { Tag = Any.Pack(new NormalTag { Name = "Cat" }) };

        var streamingCall = tagServiceClient.GetAllTagsAssociations(request);

        await foreach (var reply in streamingCall.ResponseStream.ReadAllAsync())
        {
            var t1 = string.Join(", ", reply.TagsInGroup.Select(any => _mapper.MapFromDto(any).FormattedName));
            var t2 = string.Join(", ", reply.ParentGroupNames);
            _testOutputHelper.WriteLine($"group '{reply.GroupName}':\t\t{t1}");
            _testOutputHelper.WriteLine($"\t\tancestors:  {t2}");
        }
    }

    [Fact]
    public void DeleteExistingTag_ValidRequest_ReturnsSuccessMessage()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var tag = Any.Pack(new NormalTag { Name = "NotificationTest2" });

        var request = new DeleteTagRequest { Tag = tag };

        var _ = tagServiceClient.DeleteTag(request);
    }
}
