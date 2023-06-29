using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using Xunit;

namespace TagTool.Backend.Tests.Unit;

public class TestHelper
{
    private readonly TagToolDbContext _dbContext;

    public TestHelper()
    {
        var dbContextOptions = new DbContextOptionsBuilder<TagToolDbContext>();
        dbContextOptions.UseSqlite($"Data Source={Constants.Constants.DbPath}");

        _dbContext = new TagToolDbContext(dbContextOptions.Options);
    }

    [Fact]
    public Task CreateTag_ValidRequest_ReturnsCreatedTag()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        // var tag = new MonthRangeTag { Begin = 1, End = 10 };
        // var tag = new DayRangeTag { Begin = 1, End = 4 };
        var tag = new NormalTag { Name = "Note" };

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
        var tag = new NormalTag { Name = "Note" };

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
        var tag = new NormalTag { Name = "Note" };

        var _ = tagServiceClient.UntagItem(
            new UntagItemRequest { Tag = Any.Pack(tag), File = new FileDto { Path = @"C:\Users\tczyz\MyFiles\FromOec\DigitalSign.gif" } });
        return Task.CompletedTask;
    }

    [Fact]
    public void MappingTest()
    {
        var tag = new NormalTag { Name = "Note" };
        var any = Any.Pack(tag);
    }
}
