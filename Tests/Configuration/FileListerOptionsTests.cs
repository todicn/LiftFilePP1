using ListFilePP.Configuration;
using Xunit;

namespace ListFilePP.Tests.Configuration;

/// <summary>
/// Unit tests for the FileListerOptions configuration class.
/// Tests validate default values and property behavior.
/// </summary>
public class FileListerOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new FileListerOptions();

        // Assert
        Assert.Equal(10, options.DefaultLineCount);
        Assert.Equal(1000, options.MaxLineCount);
        Assert.Equal(8192, options.BufferSize);
        Assert.False(options.ShowLineNumbers);
    }

    [Fact]
    public void DefaultLineCount_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new FileListerOptions();
        const int expectedValue = 25;

        // Act
        options.DefaultLineCount = expectedValue;

        // Assert
        Assert.Equal(expectedValue, options.DefaultLineCount);
    }

    [Fact]
    public void MaxLineCount_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new FileListerOptions();
        const int expectedValue = 2000;

        // Act
        options.MaxLineCount = expectedValue;

        // Assert
        Assert.Equal(expectedValue, options.MaxLineCount);
    }

    [Fact]
    public void BufferSize_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new FileListerOptions();
        const int expectedValue = 16384;

        // Act
        options.BufferSize = expectedValue;

        // Assert
        Assert.Equal(expectedValue, options.BufferSize);
    }

    [Fact]
    public void ShowLineNumbers_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new FileListerOptions();
        const bool expectedValue = true;

        // Act
        options.ShowLineNumbers = expectedValue;

        // Assert
        Assert.Equal(expectedValue, options.ShowLineNumbers);
    }

    [Theory]
    [InlineData(1, 10, 1024, true)]
    [InlineData(50, 500, 4096, false)]
    [InlineData(100, 10000, 65536, true)]
    public void AllProperties_CanBeSetSimultaneously(int defaultLineCount, int maxLineCount, int bufferSize, bool showLineNumbers)
    {
        // Arrange
        var options = new FileListerOptions();

        // Act
        options.DefaultLineCount = defaultLineCount;
        options.MaxLineCount = maxLineCount;
        options.BufferSize = bufferSize;
        options.ShowLineNumbers = showLineNumbers;

        // Assert
        Assert.Equal(defaultLineCount, options.DefaultLineCount);
        Assert.Equal(maxLineCount, options.MaxLineCount);
        Assert.Equal(bufferSize, options.BufferSize);
        Assert.Equal(showLineNumbers, options.ShowLineNumbers);
    }
} 