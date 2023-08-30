using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models.Tags;
using Xunit;
using Xunit.Abstractions;
using DayRangeTag = TagTool.Backend.DomainTypes.DayRangeTag;

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

        _dbContext = new TagToolDbContext(dbContextOptions.Options);
    }

    [Fact]
    public Task CreateTag_ValidRequest_ReturnsCreatedTag()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        // var tag = new MonthRangeTag { Begin = 1, End = 10 };
        var tag = new DayRangeTag { Begin = 1, End = 4 };
        // var tag = new NormalTag { Name = "College3" };

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
        var fileDto = new FileDto { Path = @"C:\Users\tczyz\MyFiles\notes.txt" };

        var _ = tagServiceClient.TagItem(new TagItemRequest { Tag = Any.Pack(tag), File = fileDto });

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
    public void UpsertTagsAssociation_ValidRequest_ReturnsSuccessMessage()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var request = new UpsertTagsAssociationRequest
        {
            FirstTag = Any.Pack(new NormalTag { Name = "Cat" }),
            SecondTag = Any.Pack(new NormalTag { Name = "Cat2" }),
            AssociationType = AssociationType.Synonyms
        };

        _ = tagServiceClient.UpsertTagsAssociation(request);
    }

    [Fact]
    public async Task GetAllTagsAssociations_ValidRequest_Returns()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var request = new GetAllTagsAssociationsRequest { Tag = Any.Pack(new NormalTag { Name = "Cat" }) };

        var streamingCall = tagServiceClient.GetAllTagsAssociations(request);

        await foreach (var reply in streamingCall.ResponseStream.ReadAllAsync())
        {
            var t1 = string.Join(", ", reply.TagSynonymsGroup.Select(any => _mapper.MapFromDto(any).FormattedName));
            var t2 = string.Join(", ", reply.HigherTags.Select(any => _mapper.MapFromDto(any).FormattedName));
            _testOutputHelper.WriteLine($"{t1}\n{t2}");
        }
    }

    [Fact]
    public async Task RemoveTagsAssociation_ValidRequest_Returns()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var request = new RemoveTagsAssociationRequest
        {
            AssociationType = AssociationType.Synonyms,
            FirstTag = Any.Pack(new NormalTag { Name = "Cat2" }),
            SecondTag = Any.Pack(new NormalTag { Name = "Cat" })
        };

        var reply = tagServiceClient.RemoveTagsAssociation(request);

        _testOutputHelper.WriteLine(reply.Error.Message);
    }

    [Fact]
    public void DeleteExistingTag_ValidRequest_ReturnsSuccessMessage()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var tag = Any.Pack(new NormalTag { Name = "Cat" });

        var request = new DeleteTagRequest { Tag = tag };

        var _ = tagServiceClient.DeleteTag(request);
    }
}
