using FluentAssertions;
using TagTool.Backend.Extensions;
using Xunit;

namespace TagTool.Backend.Tests.Unit;

public class StringExtensionsTests
{
    [Fact]
    public void GetAllSubstrings_ValidWord_CorrectReturn()
    {
        // Arrange
        var expectedSubstrings = new[] { "w", "o", "r", "d", "wo", "or", "rd", "wor", "ord", "word" };

        // Act
        var allSubstrings = "word".GetAllSubstrings();

        // Assert
        allSubstrings.Should().HaveCount(10);
        allSubstrings.Should().Equal(expectedSubstrings);
    }
}
