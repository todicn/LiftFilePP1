# ListFilePP - File Line Lister

A high-performance C# console application that displays the last N lines of a file, similar to the Unix `tail` command.

## Features

- **Efficient Memory Usage**: Uses reverse reading technique for large files to minimize memory consumption
- **Thread-Safe**: Built with concurrent access in mind using semaphores
- **Configurable**: Supports configuration through `appsettings.json` and environment variables
- **Error Handling**: Comprehensive error handling with descriptive messages
- **Performance Monitoring**: Built-in timing for debug builds
- **Async/Await**: Fully asynchronous implementation for better performance

## Usage

```bash
ListFilePP <file_path> [line_count]
```

### Arguments

- `file_path`: Path to the file to read (required)
- `line_count`: Number of lines to display from the end of the file (optional, default: 10)

### Examples

```bash
# Display last 10 lines (default)
ListFilePP myfile.txt

# Display last 20 lines
ListFilePP myfile.txt 20

# Display last 50 lines from a log file
ListFilePP "C:\logs\app.log" 50
```

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "FileLister": {
    "DefaultLineCount": 10,
    "MaxLineCount": 1000,
    "BufferSize": 8192,
    "ShowLineNumbers": false
  }
}
```

### Configuration Options

- `DefaultLineCount`: Default number of lines to display (default: 10)
- `MaxLineCount`: Maximum number of lines that can be requested (default: 1000)
- `BufferSize`: Buffer size in bytes for file reading operations (default: 8192)
- `ShowLineNumbers`: Whether to include line numbers in output (default: false)

## Architecture

The application follows clean architecture principles with:

- **Interfaces**: Define contracts for services (`IFileLister`)
- **Implementations**: Concrete implementations of interfaces (`FileLister`)
- **Configuration**: Configuration models using the Options pattern
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for IoC

### Performance Optimizations

1. **Reverse Reading**: For large files, reads backwards from the end to avoid loading the entire file
2. **Buffered I/O**: Uses configurable buffer sizes for optimal performance
3. **Memory Efficient**: LinkedList for line storage to minimize memory allocations
4. **Thread Safety**: SemaphoreSlim to control concurrent access

## Building and Running

### Prerequisites

- .NET 8.0 or later

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run -- <file_path> [line_count]
```

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## Error Handling

The application provides clear error messages for common scenarios:

- File not found
- Access denied
- Invalid arguments
- File too large (exceeds MaxLineCount)

## Exit Codes

- `0`: Success
- `1`: Error (with descriptive message)

## Development

### Project Structure

```
ListFilePP/
├── Interfaces/           # Service interfaces
├── Implementations/      # Service implementations
├── Configuration/        # Configuration models
├── Program.cs           # Main entry point
├── appsettings.json     # Configuration file
└── ListFilePP.csproj    # Project file
```

### Dependencies

- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Options

## License

This project is open source and available under the MIT License. 