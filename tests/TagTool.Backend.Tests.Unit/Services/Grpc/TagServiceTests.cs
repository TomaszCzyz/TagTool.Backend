using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Services;
using TagTool.Backend.Tests.Unit.Helpers;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public class TagServiceTests
{
    private const string TestItemType = "TestItemType";
    private const string TestItemIdentifier = "TestItemIdentifier";
    private readonly Backend.Services.Grpc.TagService _tagService;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ICommandsHistory> _commandsHistoryMock;
    private readonly TagToolDbContext _dbContextMock;

    public TagServiceTests()
    {
        var loggerMock = new Mock<ILogger<Backend.Services.Grpc.TagService>>();

        _mediatorMock = new Mock<IMediator>();
        _commandsHistoryMock = new Mock<ICommandsHistory>();

        var optionsBuilder = new DbContextOptionsBuilder<TagToolDbContext>().UseInMemoryDatabase("TagToolDb").Options;

        _dbContextMock = new TagToolDbContext(optionsBuilder);

        _tagService = new Backend.Services.Grpc.TagService(
            loggerMock.Object,
            _mediatorMock.Object,
            _commandsHistoryMock.Object);
    }

    [Fact]
    public async Task TagItem_ValidRequest_ReturnsTaggedItem()
    {
        // Arrange
        var testServerCallContext = TestServerCallContext.Create();

        var tagItemRequest
            = new TagItemRequest { File = new FileDto { Path = TestItemIdentifier }, Tag = Any.Pack(new YearTagDto { Year = 4022 }) };

        // var taggedItem = new TaggedItemDto
        // {
        //     ItemType = TestItemType,
        //     UniqueIdentifier = TestItemIdentifier,
        //     Tags = new List<TagBase>()
        // };

        // _mediatorMock
        //     .Setup(m => m.Send(It.IsAny<Commands.TagItemRequest>(), testServerCallContext.CancellationToken))
        //     .ReturnsAsync(taggedItem);

        // Act
        var response = await _tagService.TagItem(tagItemRequest, testServerCallContext);

        // Assert
        _mediatorMock.Verify(x => x.Send(It.IsAny<Commands.TagItemRequest>(), testServerCallContext.CancellationToken), Times.Once);
        // _mediatorMock.Verify(m
        //     => m.Send(
        //         It.Is<Commands.TagItemRequest>(r => r.Tag is YearTag && r.ItemType == TestItemType && r.Identifier == TestItemIdentifier),
        //         It.IsAny<CancellationToken>()));

        // response.TaggedItem.Should().Be(taggedItem);
    }
}
