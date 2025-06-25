using ListFilePP.Configuration;
using ListFilePP.Implementations;
using ListFilePP.Interfaces;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace ListFilePP.Tests.Implementations;

/// <summary>
/// Unit tests for the FileLister implementation.
/// Tests cover happy path scenarios, edge cases, and error conditions.
/// </summary>
public class FileListerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileLister _fileLister;
    private readonly FileListerOptions _options;

    public FileListerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _options = new FileListerOptions
        {
            DefaultLineCount = 10,
            MaxLineCount = 1000,
            BufferSize = 8192,
            ShowLineNumbers = false
        };

        var optionsMock = new Mock<IOptions<FileListerOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _fileLister = new FileLister(optionsMock.Object);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithValidFile_ReturnsCorrectLines()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        const int expectedLineCount = 3;

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, expectedLineCount);

        // Assert
        var lines = result.ToList();
        Assert.Equal(expectedLineCount, lines.Count);
        Assert.Equal("Line 3", lines[0]);
        Assert.Equal("Line 4", lines[1]);
        Assert.Equal("Line 5", lines[2]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithDefaultLineCount_Returns10Lines()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Range(1, 15).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath);

        // Assert
        var lines = result.ToList();
        Assert.Equal(10, lines.Count);
        Assert.Equal("Line 6", lines[0]);
        Assert.Equal("Line 15", lines[9]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithMoreLinesThanFileHas_ReturnsAllLines()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\nLine 3");
        const int requestedLines = 10;

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, requestedLines);

        // Assert
        var lines = result.ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithEmptyFile_ReturnsEmptyResult()
    {
        // Arrange
        var filePath = CreateTestFile("");

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 5);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithSingleLine_ReturnsSingleLine()
    {
        // Arrange
        var filePath = CreateTestFile("Single line content");

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 5);

        // Assert
        var lines = result.ToList();
        Assert.Single(lines);
        Assert.Equal("Single line content", lines[0]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithWindowsLineEndings_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\r\nLine 2\r\nLine 3\r\nLine 4");

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 2);

        // Assert
        var lines = result.ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("Line 3", lines[0]);
        Assert.Equal("Line 4", lines[1]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithLargeFile_UsesEfficientReading()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(1, 2000).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(largeContent);

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 5);

        // Assert
        var lines = result.ToList();
        Assert.Equal(5, lines.Count);
        Assert.Equal("Line 1996", lines[0]);
        Assert.Equal("Line 2000", lines[4]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLastLinesAsync_WithInvalidFilePath_ThrowsArgumentException(string? filePath)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _fileLister.GetLastLinesAsync(filePath!, 10));
        
        Assert.Contains("File path cannot be null or empty", exception.Message);
        Assert.Equal("filePath", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetLastLinesAsync_WithInvalidLineCount_ThrowsArgumentException(int lineCount)
    {
        // Arrange
        var filePath = CreateTestFile("Test content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _fileLister.GetLastLinesAsync(filePath, lineCount));
        
        Assert.Contains("Line count must be greater than zero", exception.Message);
        Assert.Equal("lineCount", exception.ParamName);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithLineCountExceedingMax_ThrowsArgumentException()
    {
        // Arrange
        var filePath = CreateTestFile("Test content");
        var excessiveLineCount = _options.MaxLineCount + 1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _fileLister.GetLastLinesAsync(filePath, excessiveLineCount));
        
        Assert.Contains($"Line count cannot exceed {_options.MaxLineCount}", exception.Message);
        Assert.Equal("lineCount", exception.ParamName);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _fileLister.GetLastLinesAsync(nonExistentPath, 10));
        
        Assert.Contains($"The file '{nonExistentPath}' was not found", exception.Message);
        Assert.Equal(nonExistentPath, exception.FileName);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithCancellationToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var filePath = CreateTestFile("Test content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _fileLister.GetLastLinesAsync(filePath, 10, cts.Token));
    }

    [Fact]
    public async Task GetLastLinesAsync_WithTrailingNewlines_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\nLine 3\n\n\n");

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 5);

        // Assert
        var lines = result.ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 2", lines[1]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithMixedLineEndings_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\r\nLine 3\rLine 4");

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 4);

        // Assert
        var lines = result.ToList();
        Assert.Equal(4, lines.Count);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithUnicodeContent_HandlesCorrectly()
    {
        // Arrange
        var unicodeContent = "Линия 1\nЛиния 2\n日本語\n한국어\n中文";
        var filePath = CreateTestFile(unicodeContent);

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 3);

        // Assert
        var lines = result.ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal("日本語", lines[0]);
        Assert.Equal("한국어", lines[1]);
        Assert.Equal("中文", lines[2]);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new FileLister(null!));
        
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task GetLastLinesAsync_MultipleConcurrentCalls_HandlesThreadSafety()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        var tasks = new List<Task<IEnumerable<string>>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_fileLister.GetLastLinesAsync(filePath, 5));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            var lines = result.ToList();
            Assert.Equal(5, lines.Count);
            Assert.Equal("Line 96", lines[0]);
            Assert.Equal("Line 100", lines[4]);
        }
    }

    private string CreateTestFile(string content)
    {
        var filePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.txt");
        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    public void Dispose()
    {
        _fileLister?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
} 