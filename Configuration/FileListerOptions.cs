namespace ListFilePP.Configuration;

/// <summary>
/// Configuration options for the file lister application.
/// </summary>
public class FileListerOptions
{
    /// <summary>
    /// The default number of lines to display when no count is specified.
    /// </summary>
    public int DefaultLineCount { get; set; } = 10;

    /// <summary>
    /// The maximum number of lines that can be requested at once.
    /// </summary>
    public int MaxLineCount { get; set; } = 1000;

    /// <summary>
    /// The buffer size in bytes used for reading files efficiently.
    /// </summary>
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    /// Whether to include line numbers in the output.
    /// </summary>
    public bool ShowLineNumbers { get; set; } = false;
} 