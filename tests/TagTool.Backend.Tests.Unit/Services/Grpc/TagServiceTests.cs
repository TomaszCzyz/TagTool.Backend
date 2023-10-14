using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Services;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    private const string GroupName = "groupName";
    private const string ArbitraryErrorMessage = "Arbitrary error message.";
    private const string ArbitrarySuccessMessage = "Arbitrary success message.";

    private readonly Backend.Services.Grpc.TagService _sut;

    private readonly ILogger<Backend.Services.Grpc.TagService>? _loggerMock = Substitute.For<ILogger<Backend.Services.Grpc.TagService>>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICommandsHistory _commandsHistory = Substitute.For<ICommandsHistory>();
    private readonly ITagMapper _tagMapper = TagMapperHelper.InitializeWithKnownMappers();
    private readonly ITaggableItemMapper _taggableItemMapper = new TaggableItemMapper();
    private readonly TestServerCallContext _testServerCallContext = TestServerCallContext.Create();

    private readonly FileDto _fileDto = new() { Path = "TestFileDtoIdentifier" };
    private readonly FolderDto _folderDto = new() { Path = "TestFolderDtoIdentifier" };
    private readonly NormalTag _textTag = new() { Name = "TestTag1" };
    private readonly DayTag _dayTag = new() { Day = (int)DayOfWeek.Monday };

    public TagServiceTests()
    {
        _sut = new Backend.Services.Grpc.TagService(
            _loggerMock,
            _mediator,
            _commandsHistory,
            _tagMapper,
            _taggableItemMapper);
    }
}
