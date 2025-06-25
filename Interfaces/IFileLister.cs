namespace ListFilePP.Interfaces;

/// <summary>
/// Interface for listing lines from files.
/// </summary>
public interface IFileLister : IDisposable
{
    /// <summary>
    /// Retrieves the last N lines from the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to retrieve from the end of the file. Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the lines from the file.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    Task<IEnumerable<string>> GetLastLinesAsync(string filePath, int lineCount = 10, CancellationToken cancellationToken = default);
} 