using ListFilePP.Configuration;
using ListFilePP.Implementations;
using ListFilePP.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ListFilePP;

/// <summary>
/// Main program class for the ListFilePP application.
/// Implements a command-line tool to display the last N lines of a file.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments. First argument should be the file path, optional second argument is the number of lines.</param>
    /// <returns>Exit code: 0 for success, 1 for error.</returns>
    public static async Task<int> Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Parse command line arguments
            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var filePath = args[0];
            var lineCount = 10; // Default to 10 lines

            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out lineCount) || lineCount <= 0)
                {
                    Console.WriteLine("Error: Line count must be a positive integer.");
                    ShowUsage();
                    return 1;
                }
            }

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);

            using var serviceProvider = services.BuildServiceProvider();
            using var fileLister = serviceProvider.GetRequiredService<IFileLister>();

            // Execute the file listing operation
            var lines = await fileLister.GetLastLinesAsync(filePath, lineCount);
            
            // Display the results
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            stopwatch.Stop();
            
            // Show timing information in debug mode
            #if DEBUG
            Console.WriteLine($"Operation completed in {stopwatch.ElapsedMilliseconds}ms");
            #endif

            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            ShowUsage();
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: File not found - {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Error: Access denied - {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            #if DEBUG
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            #endif
            return 1;
        }
    }

    /// <summary>
    /// Configures the dependency injection services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Register configuration
        services.Configure<FileListerOptions>(options =>
        {
            configuration.GetSection("FileLister").Bind(options);
        });
        
        // Register services
        services.AddSingleton<IFileLister, FileLister>();
    }

    /// <summary>
    /// Shows the usage information for the application.
    /// </summary>
    private static void ShowUsage()
    {
        Console.WriteLine("ListFilePP - Display the last lines of a file");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  ListFilePP <file_path> [line_count]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  file_path   Path to the file to read");
        Console.WriteLine("  line_count  Number of lines to display (default: 10)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ListFilePP myfile.txt");
        Console.WriteLine("  ListFilePP myfile.txt 20");
        Console.WriteLine("  ListFilePP \"C:\\logs\\app.log\" 50");
    }
} 