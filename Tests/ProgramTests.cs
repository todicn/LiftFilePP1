using ListFilePP.Configuration;
using ListFilePP.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;
using Xunit;

namespace ListFilePP.Tests;

/// <summary>
/// Integration tests for the Program class and overall application flow.
/// Tests cover dependency injection setup, service registration, and end-to-end scenarios.
/// </summary>
public class ProgramTests : IDisposable
{
    private readonly string _testDirectory;

    public ProgramTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task Main_WithValidFileAndDefaultLineCount_ReturnsSuccessExitCode()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        var args = new[] { filePath };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithValidFileAndCustomLineCount_ReturnsSuccessExitCode()
    {
        // Arrange
        var filePath = CreateTestFile("Line 1\nLine 2\nLine 3\nLine 4\nLine 5");
        var args = new[] { filePath, "3" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithNoArguments_ReturnsErrorExitCode()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithNonExistentFile_ReturnsErrorExitCode()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");
        var args = new[] { nonExistentPath };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithInvalidLineCount_ReturnsErrorExitCode()
    {
        // Arrange
        var filePath = CreateTestFile("Test content");
        var args = new[] { filePath, "invalid" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithNegativeLineCount_ReturnsErrorExitCode()
    {
        // Arrange
        var filePath = CreateTestFile("Test content");
        var args = new[] { filePath, "-5" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithZeroLineCount_ReturnsErrorExitCode()
    {
        // Arrange
        var filePath = CreateTestFile("Test content");
        var args = new[] { filePath, "0" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void ConfigureServices_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Use reflection to call the private ConfigureServices method
        var programType = typeof(Program);
        var method = programType.GetMethod("ConfigureServices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        method?.Invoke(null, new object[] { services });
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var fileLister = serviceProvider.GetService<IFileLister>();
        Assert.NotNull(fileLister);

        var options = serviceProvider.GetService<IOptions<FileListerOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void ConfigureServices_ConfiguresOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Use reflection to call the private ConfigureServices method
        var programType = typeof(Program);
        var method = programType.GetMethod("ConfigureServices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        method?.Invoke(null, new object[] { services });
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<FileListerOptions>>();
        Assert.Equal(10, options.Value.DefaultLineCount);
        Assert.Equal(1000, options.Value.MaxLineCount);
        Assert.Equal(8192, options.Value.BufferSize);
        Assert.False(options.Value.ShowLineNumbers);
    }

    [Fact]
    public void ConfigureServices_RegistersFileListerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Use reflection to call the private ConfigureServices method
        var programType = typeof(Program);
        var method = programType.GetMethod("ConfigureServices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        method?.Invoke(null, new object[] { services });
        using var serviceProvider = services.BuildServiceProvider();

        // Assert
        var fileLister1 = serviceProvider.GetRequiredService<IFileLister>();
        var fileLister2 = serviceProvider.GetRequiredService<IFileLister>();
        
        Assert.Same(fileLister1, fileLister2);
    }

    [Theory]
    [InlineData("testfile.txt", "5")]
    [InlineData("another-file.txt", "10")]
    [InlineData("C:\\temp\\file.log", "20")]
    public async Task Main_WithValidArguments_ParsesCorrectly(string fileName, string lineCount)
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, Path.GetFileName(fileName));
        CreateTestFileAt(testFile, "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10\nLine 11\nLine 12");
        var args = new[] { testFile, lineCount };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithLargeFile_HandlesEfficiently()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(1, 5000).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(largeContent);
        var args = new[] { filePath, "100" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithEmptyFile_HandlesGracefully()
    {
        // Arrange
        var filePath = CreateTestFile("");
        var args = new[] { filePath, "10" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithSingleLineFile_HandlesCorrectly()
    {
        // Arrange
        var filePath = CreateTestFile("Single line of content");
        var args = new[] { filePath, "5" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WithFileContainingUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var unicodeContent = "Hello 世界\nBonjour 🌍\nHola 🌎\nGuten Tag 🌏";
        var filePath = CreateTestFile(unicodeContent);
        var args = new[] { filePath, "2" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    private string CreateTestFile(string content)
    {
        var filePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.txt");
        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    private void CreateTestFileAt(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
} 