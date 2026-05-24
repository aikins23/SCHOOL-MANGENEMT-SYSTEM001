using System;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Console program to initialize the database schema
    /// Run this to create the StudentTermRemarks table and other required tables
    /// </summary>
    class InitDatabaseProgram
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Database Initialization Utility");
            Console.WriteLine("========================================\n");

            try
            {
                Console.WriteLine("Step 1: Initializing database schema...");
                bool initSuccess = await DatabaseInitializer.InitializeDatabaseAsync();

                if (!initSuccess)
                {
                    Console.WriteLine("\nERROR: Database initialization failed!");
                    Environment.Exit(1);
                }

                Console.WriteLine("\nStep 2: Verifying StudentTermRemarks table...");
                bool verifySuccess = await DatabaseInitializer.VerifyStudentTermRemarksTableAsync();

                if (!verifySuccess)
                {
                    Console.WriteLine("\nERROR: StudentTermRemarks table verification failed!");
                    Environment.Exit(1);
                }

                Console.WriteLine("\n========================================");
                Console.WriteLine("SUCCESS: Database initialization complete!");
                Console.WriteLine("========================================");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
