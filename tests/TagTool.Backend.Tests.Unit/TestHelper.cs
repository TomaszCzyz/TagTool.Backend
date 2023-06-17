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
    public async Task CreateTag_ValidRequest_ReturnsCreatedTag()
    {
        var tagServiceClient = new TagService.TagServiceClient(UnixDomainSocketConnectionFactory.CreateChannel());

        var tag = new MonthRangeTag { Begin = 1, End = 10 };
        // var tag = new DayRangeTag { Begin = 1, End = 4 };

        var _ = tagServiceClient.CreateTag(new CreateTagRequest { Tag = Any.Pack(tag) });
    }
}
