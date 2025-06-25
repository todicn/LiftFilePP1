using ListFilePP.Configuration;
using ListFilePP.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace ListFilePP.Implementations;

/// <summary>
/// Implementation of the file lister service that efficiently reads the last N lines from files.
/// This implementation is thread-safe and uses memory-efficient techniques for large files.
/// </summary>
public class FileLister : IFileLister
{
    private readonly FileListerOptions _options;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Initializes a new instance of the FileLister class.
    /// </summary>
    /// <param name="options">Configuration options for the file lister.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public FileLister(IOptions<FileListerOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetLastLinesAsync(string filePath, int lineCount = 10, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (lineCount <= 0)
        {
            throw new ArgumentException("Line count must be greater than zero.", nameof(lineCount));
        }

        if (lineCount > _options.MaxLineCount)
        {
            throw new ArgumentException($"Line count cannot exceed {_options.MaxLineCount}.", nameof(lineCount));
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.", filePath);
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await ReadLastLinesInternalAsync(filePath, lineCount, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Internal method to read the last N lines from a file efficiently.
    /// Uses reverse reading technique to minimize memory usage for large files.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The last N lines from the file.</returns>
    private async Task<IEnumerable<string>> ReadLastLinesInternalAsync(string filePath, int lineCount, CancellationToken cancellationToken)
    {
        var lines = new List<string>(lineCount);
        
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.BufferSize, useAsync: true);
        
        // If the file is small enough, read it all at once
        if (fileStream.Length <= _options.BufferSize)
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var allLines = new List<string>();
            string? line;
            
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                allLines.Add(line);
            }
            
            return allLines.Skip(Math.Max(0, allLines.Count - lineCount));
        }

        // For larger files, use reverse reading approach
        return await ReadLastLinesReversedAsync(fileStream, lineCount, cancellationToken);
    }

    /// <summary>
    /// Reads the last N lines from a large file by reading backwards from the end.
    /// This approach is memory-efficient for large files.
    /// </summary>
    /// <param name="fileStream">The file stream to read from.</param>
    /// <param name="lineCount">The number of lines to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The last N lines from the file.</returns>
    private async Task<IEnumerable<string>> ReadLastLinesReversedAsync(FileStream fileStream, int lineCount, CancellationToken cancellationToken)
    {
        var lines = new LinkedList<string>();
        var buffer = new byte[_options.BufferSize];
        var lineBuilder = new StringBuilder();
        var position = fileStream.Length;
        var foundLines = 0;

        while (position > 0 && foundLines < lineCount)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate how much to read
            var readSize = (int)Math.Min(_options.BufferSize, position);
            position -= readSize;

            // Read the chunk
            fileStream.Seek(position, SeekOrigin.Begin);
            await fileStream.ReadAsync(buffer, 0, readSize, cancellationToken);

            // Process bytes in reverse order
            for (int i = readSize - 1; i >= 0; i--)
            {
                var b = buffer[i];
                
                if (b == '\n' || b == '\r')
                {
                    if (lineBuilder.Length > 0)
                    {
                        lines.AddFirst(ReverseString(lineBuilder.ToString()));
                        lineBuilder.Clear();
                        foundLines++;
                        
                        if (foundLines >= lineCount)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    lineBuilder.Append((char)b);
                }
            }
        }

        // Add any remaining content as the first line
        if (lineBuilder.Length > 0 && foundLines < lineCount)
        {
            lines.AddFirst(ReverseString(lineBuilder.ToString()));
        }

        return lines;
    }

    /// <summary>
    /// Reverses a string efficiently.
    /// </summary>
    /// <param name="str">The string to reverse.</param>
    /// <returns>The reversed string.</returns>
    private static string ReverseString(string str)
    {
        var chars = str.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// Disposes of the resources used by the FileLister.
    /// </summary>
    public void Dispose()
    {
        _semaphore?.Dispose();
    }
} 