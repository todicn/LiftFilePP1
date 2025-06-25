using ListFilePP.Configuration;
using ListFilePP.Implementations;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ListFilePP.Tests;

/// <summary>
/// Performance tests to validate the efficiency of file reading operations.
/// These tests ensure the application handles large files and high-throughput scenarios effectively.
/// </summary>
public class PerformanceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ITestOutputHelper _output;
    private readonly FileLister _fileLister;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var options = new FileListerOptions
        {
            DefaultLineCount = 10,
            MaxLineCount = 1000,
            BufferSize = 8192,
            ShowLineNumbers = false
        };

        var optionsMock = new Mock<IOptions<FileListerOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        _fileLister = new FileLister(optionsMock.Object);
    }

    [Fact]
    public async Task GetLastLinesAsync_WithLargeFile_CompletesWithinTimeLimit()
    {
        // Arrange
        var lineCount = 100000;
        var content = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"Line {i} with some additional content to make it longer"));
        var filePath = CreateTestFile(content);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 100);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Processing {lineCount} lines took {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Expected operation to complete within 5 seconds, but took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal(100, result.Count());
    }

    [Fact]
    public async Task GetLastLinesAsync_WithVeryLargeFile_UsesConstantMemory()
    {
        // Arrange
        var lineCount = 1000000; // 1 million lines
        var content = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 50);

        // Assert
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
        
        _output.WriteLine($"Processing {lineCount} lines took {stopwatch.ElapsedMilliseconds}ms and used {memoryUsed / 1024 / 1024}MB of memory");
        
        Assert.True(memoryUsed < 100 * 1024 * 1024, $"Expected memory usage < 100MB, but used {memoryUsed / 1024 / 1024}MB");
        Assert.Equal(50, result.Count());
    }

    [Fact]
    public async Task GetLastLinesAsync_MultipleConcurrentReads_HandlesLoadEfficiently()
    {
        // Arrange
        var lineCount = 50000;
        var content = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        var concurrentRequests = 20;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => _fileLister.GetLastLinesAsync(filePath, 25))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Processing {concurrentRequests} concurrent requests took {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Expected concurrent operations to complete within 10 seconds, but took {stopwatch.ElapsedMilliseconds}ms");
        Assert.All(results, result => Assert.Equal(25, result.Count()));
    }

    [Theory]
    [InlineData(1024)]     // 1KB buffer
    [InlineData(4096)]     // 4KB buffer
    [InlineData(8192)]     // 8KB buffer (default)
    [InlineData(16384)]    // 16KB buffer
    [InlineData(65536)]    // 64KB buffer
    public async Task GetLastLinesAsync_WithDifferentBufferSizes_MaintainsPerformance(int bufferSize)
    {
        // Arrange
        var options = new FileListerOptions
        {
            DefaultLineCount = 10,
            MaxLineCount = 1000,
            BufferSize = bufferSize,
            ShowLineNumbers = false
        };

        var optionsMock = new Mock<IOptions<FileListerOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        using var fileLister = new FileLister(optionsMock.Object);
        
        var lineCount = 100000;
        var content = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await fileLister.GetLastLinesAsync(filePath, 100);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Buffer size {bufferSize} bytes took {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"Expected operation with buffer size {bufferSize} to complete within 3 seconds");
        Assert.Equal(100, result.Count());
    }

    [Fact]
    public async Task GetLastLinesAsync_WithSmallFile_OptimizesForDirectReading()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 50);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Small file processing took {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Expected small file operation to complete within 100ms, but took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal(50, result.Count());
    }

    [Fact]
    public async Task GetLastLinesAsync_WithLongLines_HandlesEfficiently()
    {
        // Arrange
        var longLineContent = new string('X', 10000); // 10KB per line
        var content = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}: {longLineContent}"));
        var filePath = CreateTestFile(content);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _fileLister.GetLastLinesAsync(filePath, 10);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Long lines processing took {stopwatch.ElapsedMilliseconds}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Expected long lines operation to complete within 2 seconds, but took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal(10, result.Count());
        Assert.All(result, line => Assert.Contains("Line", line));
    }

    [Fact]
    public async Task GetLastLinesAsync_WithManySmallRequests_MaintainsConsistentPerformance()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i}"));
        var filePath = CreateTestFile(content);
        var requestCount = 100;
        var executionTimes = new List<long>();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _fileLister.GetLastLinesAsync(filePath, 5);
            stopwatch.Stop();
            
            executionTimes.Add(stopwatch.ElapsedMilliseconds);
            Assert.Equal(5, result.Count());
        }

        // Assert
        var averageTime = executionTimes.Average();
        var maxTime = executionTimes.Max();
        var minTime = executionTimes.Min();
        
        _output.WriteLine($"Average execution time: {averageTime:F2}ms, Min: {minTime}ms, Max: {maxTime}ms");
        
        Assert.True(averageTime < 50, $"Expected average execution time < 50ms, but got {averageTime:F2}ms");
        Assert.True(maxTime < 200, $"Expected max execution time < 200ms, but got {maxTime}ms");
    }

    private string CreateTestFile(string content)
    {
        var filePath = Path.Combine(_testDirectory, $"perf_test_{Guid.NewGuid()}.txt");
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