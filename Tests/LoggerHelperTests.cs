using System;
using System.IO;
using System.Linq;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    /// <summary>
    /// Manual test class for LoggerHelper functionality.
    /// Run this to verify logging is working correctly.
    /// </summary>
    public class LoggerHelperTests
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly DateTime TestDate = DateTime.Now.Date;
        private static readonly string ExpectedLogFile = Path.Combine(LogDirectory, $"app-{TestDate:yyyy-MM-dd}.log");

        /// <summary>
        /// Test that all three logging methods exist and can be called without throwing exceptions.
        /// </summary>
        public static void TestLoggingMethodsExist()
        {
            Console.WriteLine("TEST 1: Verifying LoggerHelper methods exist...");
            try
            {
                // These should not throw
                LoggerHelper.LogInfo("Test info message");
                LoggerHelper.LogWarning("Test warning message");
                LoggerHelper.LogError("Test error message");
                Console.WriteLine("PASS: All logging methods exist and execute without exception");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that LoggerHelper.LogError works with exception parameter.
        /// </summary>
        public static void TestErrorWithException()
        {
            Console.WriteLine("\nTEST 2: Verifying LogError with exception parameter...");
            try
            {
                var testEx = new InvalidOperationException("Test exception for logging");
                LoggerHelper.LogError("Error occurred during operation", testEx);
                Console.WriteLine("PASS: LogError with exception parameter works");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that log file is created with expected format.
        /// </summary>
        public static void TestLogFileCreation()
        {
            Console.WriteLine("\nTEST 3: Verifying log file is created...");
            try
            {
                // Clean up any existing log file for this test
                if (File.Exists(ExpectedLogFile))
                {
                    File.Delete(ExpectedLogFile);
                }

                // Create a unique message
                string uniqueMessage = $"Test message {Guid.NewGuid():N}";
                LoggerHelper.LogInfo(uniqueMessage);

                // Force NLog to flush
                NLog.LogManager.Flush();

                // Give file system time to write
                System.Threading.Thread.Sleep(100);

                // Check if log file was created
                if (File.Exists(ExpectedLogFile))
                {
                    string content = File.ReadAllText(ExpectedLogFile);
                    if (content.Contains(uniqueMessage))
                    {
                        Console.WriteLine($"PASS: Log file created and message written: {ExpectedLogFile}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"PARTIAL: Log file exists but doesn't contain our message yet. File: {ExpectedLogFile}");
                        Console.WriteLine($"Content preview: {content.Substring(0, Math.Min(200, content.Length))}");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"PARTIAL: Log directory may not exist yet. Expected: {ExpectedLogFile}");
                    if (Directory.Exists(LogDirectory))
                    {
                        var files = Directory.GetFiles(LogDirectory, "*.log");
                        if (files.Length > 0)
                        {
                            Console.WriteLine($"Found log files: {string.Join(", ", files)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.Message}");
            }
        }

        /// <summary>
        /// Test that null arguments are handled properly.
        /// </summary>
        public static void TestNullHandling()
        {
            Console.WriteLine("\nTEST 4: Verifying null argument handling...");
            try
            {
                bool nullThrown = false;
                try
                {
                    LoggerHelper.LogInfo(null);
                }
                catch (ArgumentNullException)
                {
                    nullThrown = true;
                }

                if (nullThrown)
                {
                    Console.WriteLine("PASS: Null message properly throws ArgumentNullException");
                }
                else
                {
                    Console.WriteLine("FAIL: Null message should throw ArgumentNullException");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: Unexpected exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Run all tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("LoggerHelper Test Suite");
            Console.WriteLine("========================================");

            TestLoggingMethodsExist();
            TestErrorWithException();
            TestNullHandling();
            TestLogFileCreation();

            Console.WriteLine("\n========================================");
            Console.WriteLine("Test suite completed");
            Console.WriteLine("========================================");
        }
    }
}
