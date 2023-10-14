using FluentAssertions;
using NSubstitute;
using OneOf.Types;
using TagTool.Backend.Commands;
using TagTool.Backend.Models;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task SetTagNamingConvention_ValidRequest_Succeed_ReturnsEmpty()
    {
        // Arrange
        var request = new SetTagNamingConventionRequest { Convention = NamingConvention.CamelCase };

        _mediator.Send(Arg.Any<SetTagNamingConventionCommand>()).Returns(_ => new None());

        // Act
        var response = await _sut.SetTagNamingConvention(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<SetTagNamingConventionCommand>());
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task SetTagNamingConvention_ValidRequest_Failed_ReturnsError()
    {
        // Arrange
        var request = new SetTagNamingConventionRequest { Convention = NamingConvention.CamelCase };

        _mediator.Send(Arg.Any<SetTagNamingConventionCommand>()).Returns(_ => new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.SetTagNamingConvention(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<SetTagNamingConventionCommand>());
        response.Error.Should().NotBeNull();
        response.Error.Message.Should().NotBeNull().And.Be(ArbitraryErrorMessage);
    }
}
