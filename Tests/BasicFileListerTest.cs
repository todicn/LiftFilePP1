using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ListFilePP.Implementations;
using ListFilePP.Interfaces;
using ListFilePP.Configuration;
using Microsoft.Extensions.Options;

namespace Tests
{
    public class BasicFileListerTest
    {
        public static async Task<bool> TestBasicFunctionality()
        {
            try
            {
                // Create a test file
                var testFile = Path.GetTempFileName();
                var lines = new string[]
                {
                    "Line 1",
                    "Line 2", 
                    "Line 3",
                    "Line 4",
                    "Line 5",
                    "Line 6",
                    "Line 7",
                    "Line 8",
                    "Line 9",
                    "Line 10",
                    "Line 11",
                    "Line 12"
                };
                
                await File.WriteAllLinesAsync(testFile, lines);
                
                // Test the FileLister
                var options = Options.Create(new FileListerOptions());
                IFileLister fileLister = new FileLister(options);
                
                var result = await fileLister.GetLastLinesAsync(testFile, 5);
                var resultList = result.ToList();
                
                // Clean up
                File.Delete(testFile);
                
                // Verify we got the last 5 lines
                if (resultList.Count == 5 && resultList[0] == "Line 8" && resultList[4] == "Line 12")
                {
                    Console.WriteLine("✓ Basic FileLister test passed");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ Basic FileLister test failed. Expected 5 lines, got {resultList.Count}");
                    Console.WriteLine($"First line: {resultList.FirstOrDefault()}, Last line: {resultList.LastOrDefault()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Basic FileLister test failed with exception: {ex.Message}");
                return false;
            }
        }
    }
} 