using System;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;
using System.Collections.Generic;
using System.Linq;

namespace kingdom_Preparatory_School_Management_System
{
    public static class DataSeeder
    {
        public static async Task SeedEmployeesAndUsersAsync()
        {
            try
            {
                var employeeRepo = new EmployeeRepository(Common.AppConfig.ConnectionString);
                
                // Check if we already have employees to avoid duplicates
                var existing = await employeeRepo.GetAllAsync();
                if (existing != null && existing.Count() > 0)
                {
                    Console.WriteLine("Data already seeded.");
                    return;
                }

                // 1. Create Employees
                var employees = new List<Employee>
                {
                    new Employee { FullName = "Dr. Emmanuel Aikins", Gender = "Male", DateOfBirth = new DateTime(1980, 5, 12), Contact = "0241234567", Email = "principal@nyansapoerp.edu.gh", Department = "Administration", Position = "Principal", HomeTown = "Accra", Residence = "Accra", EmploymentDate = new DateTime(2015, 1, 1), EmploymentMode = "Full-Time", EmploymentStatus = "Active", EmergencyContactPerson = "Mrs. Aikins", EmergencyContact = "0247654321", PerformanceReview = "Excellent", Salary = 5000m },
                    new Employee { FullName = "Sarah Mensah", Gender = "Female", DateOfBirth = new DateTime(1985, 8, 22), Contact = "0509876543", Email = "bursar@nyansapoerp.edu.gh", Department = "Accounts", Position = "Bursar", HomeTown = "Kumasi", Residence = "Accra", EmploymentDate = new DateTime(2018, 3, 15), EmploymentMode = "Full-Time", EmploymentStatus = "Active", EmergencyContactPerson = "Mr. Mensah", EmergencyContact = "0501234987", PerformanceReview = "Good", Salary = 3500m },
                    new Employee { FullName = "Kwame Osei", Gender = "Male", DateOfBirth = new DateTime(1990, 11, 2), Contact = "0201122334", Email = "kwame.osei@nyansapoerp.edu.gh", Department = "Teaching", Position = "Teacher", HomeTown = "Cape Coast", Residence = "Accra", EmploymentDate = new DateTime(2020, 9, 1), EmploymentMode = "Full-Time", EmploymentStatus = "Active", EmergencyContactPerson = "Abena Osei", EmergencyContact = "0204433221", PerformanceReview = "Very Good", Salary = 2500m },
                    new Employee { FullName = "Abigail Ofori", Gender = "Female", DateOfBirth = new DateTime(1992, 4, 18), Contact = "0545566778", Email = "abigail.ofori@nyansapoerp.edu.gh", Department = "Teaching", Position = "Teacher", HomeTown = "Tema", Residence = "Accra", EmploymentDate = new DateTime(2021, 9, 1), EmploymentMode = "Full-Time", EmploymentStatus = "Active", EmergencyContactPerson = "Kofi Ofori", EmergencyContact = "0548877665", PerformanceReview = "Excellent", Salary = 2500m },
                    new Employee { FullName = "John Doe", Gender = "Male", DateOfBirth = new DateTime(1988, 7, 7), Contact = "0279988776", Email = "john.doe@nyansapoerp.edu.gh", Department = "Security", Position = "Security Guard", HomeTown = "Ho", Residence = "Accra", EmploymentDate = new DateTime(2019, 5, 10), EmploymentMode = "Contract", EmploymentStatus = "Active", EmergencyContactPerson = "Jane Doe", EmergencyContact = "0276677889", PerformanceReview = "Good", Salary = 1200m }
                };

                foreach (var emp in employees)
                {
                    await employeeRepo.AddAsync(emp);
                }

                // Retrieve them again to get the generated IDs
                existing = await employeeRepo.GetAllAsync();

                // 2. Create Users for these Employees
                await AuthService.EnsureDatabaseSetupAsync();

                string seedPassword = Environment.GetEnvironmentVariable("NYANSAPO_SEED_USER_PASSWORD");
                if (string.IsNullOrWhiteSpace(seedPassword))
                {
                    throw new InvalidOperationException(
                        "Set NYANSAPO_SEED_USER_PASSWORD before creating demonstration user accounts.");
                }

                foreach (var emp in existing)
                {
                    if (string.IsNullOrEmpty(emp.EmployeeID)) continue;
                    
                    int empId = int.Parse(emp.EmployeeID);
                    string username = emp.FullName.Split(' ')[0].ToLower(); // e.g. "emmanuel", "sarah"
                    string userType = "TEACHER";

                    if (emp.Position == "Principal") 
                    {
                        username = "admin"; 
                        userType = "ADMINISTRATOR"; 
                    }
                    else if (emp.Position == "Bursar") 
                    {
                        username = "bursar"; 
                        userType = "ACCOUNTANT"; 
                    }
                    else if (emp.Position == "Security Guard")
                    {
                        continue; // No login needed
                    }

                    // Attempt registration
                    await AuthService.RegisterAsync(username, seedPassword, seedPassword, userType, empId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error seeding data: " + ex.Message);
            }
        }
    }
}
