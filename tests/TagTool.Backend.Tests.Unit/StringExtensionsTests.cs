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
        var allSubstrings = "word".Substrings();

        // Assert
        allSubstrings.Should().HaveCount(10);
        allSubstrings.Should().Equal(expectedSubstrings);
    }

    [Fact]
    public void ContainsPath_ShouldReturnTrue_WhenPathIsSupDirectory()
    {
        // Arrange
        var path = @"C:\Projects\ProjectA".AsSpan();
        var entryPath = @"C:\Projects\ProjectA\Code".AsSpan();

        var i = entryPath.LastIndexOf('\\');
        var parentDir = entryPath[..i];
        var dirName = entryPath[(i + 1)..];

        // Act
        var result = path.ContainsPath(parentDir, dirName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsPath_ShouldReturnFalse_WhenPathIsNotSupDirectory1()
    {
        // Arrange
        var path = @"C:\Projects\ProjectA\OtherCode".AsSpan();
        var entryPath = @"C:\Projects\ProjectA\Code".AsSpan();

        var i = entryPath.LastIndexOf('\\');
        var parentDir = entryPath[..i];
        var dirName = entryPath[(i + 1)..];

        // Act
        var result = path.ContainsPath(parentDir, dirName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsPath_ShouldReturnFalse_WhenPathIsNotSupDirectory()
    {
        // Arrange
        var path = @"C:\Projects\ProjectB\".AsSpan();
        var entryPath = @"C:\Projects\ProjectA\Code".AsSpan();

        var i = entryPath.LastIndexOf('\\');
        var parentDir = entryPath[..i];
        var dirName = entryPath[(i + 1)..];

        // Act
        var result = path.ContainsPath(parentDir, dirName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsPath_ShouldReturnTrue_WhenPathsAreEqual()
    {
        // Arrange
        var path = @"C:\Projects\ProjectA\Code".AsSpan();
        var entryPath = @"C:\Projects\ProjectA\Code".AsSpan();

        var i = entryPath.LastIndexOf('\\');
        var parentDir = entryPath[..i];
        var dirName = entryPath[(i + 1)..];

        // Act
        var result = path.ContainsPath(parentDir, dirName);

        // Assert
        Assert.True(result);
    }
}
