using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ListFilePP.Implementations;
using ListFilePP.Interfaces;
using ListFilePP.Configuration;
using Microsoft.Extensions.Options;

namespace TestApp
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Testing ListFilePP Functionality...");
            Console.WriteLine("===================================");
            
            try
            {
                // Create a test file
                var testFile = Path.GetTempFileName();
                var lines = Enumerable.Range(1, 15).Select(i => $"Test Line {i}").ToArray();
                
                await File.WriteAllLinesAsync(testFile, lines);
                Console.WriteLine($"Created test file with {lines.Length} lines");
                
                // Test the FileLister
                var options = Options.Create(new FileListerOptions());
                using IFileLister fileLister = new FileLister(options);
                
                Console.WriteLine("\nTesting GetLastLinesAsync with default count (10):");
                var result = await fileLister.GetLastLinesAsync(testFile);
                var resultList = result.ToList();
                
                foreach (var line in resultList)
                {
                    Console.WriteLine($"  {line}");
                }
                
                Console.WriteLine($"\nResult: Got {resultList.Count} lines (expected 10)");
                
                // Test with custom count
                Console.WriteLine("\nTesting GetLastLinesAsync with count = 5:");
                var result5 = await fileLister.GetLastLinesAsync(testFile, 5);
                var result5List = result5.ToList();
                
                foreach (var line in result5List)
                {
                    Console.WriteLine($"  {line}");
                }
                
                Console.WriteLine($"\nResult: Got {result5List.Count} lines (expected 5)");
                
                // Clean up
                File.Delete(testFile);
                
                // Verify results
                bool success = resultList.Count == 10 && 
                              resultList[0] == "Test Line 6" && 
                              resultList[9] == "Test Line 15" &&
                              result5List.Count == 5 &&
                              result5List[0] == "Test Line 11" &&
                              result5List[4] == "Test Line 15";
                
                if (success)
                {
                    Console.WriteLine("\n✅ All tests passed!");
                    return 0;
                }
                else
                {
                    Console.WriteLine("\n❌ Tests failed!");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed with exception: {ex.Message}");
                return 1;
            }
        }
    }
} 