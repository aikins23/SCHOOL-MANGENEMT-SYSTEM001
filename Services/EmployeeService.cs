using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Business logic service for employee operations
    /// </summary>
    public class EmployeeService
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<(bool Success, string Message)> AddEmployeeAsync(Models.Employee employee)
        {
            try
            {
                var validationResult = ValidateEmployee(employee);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.ErrorMessage);
                }

                var result = await _repository.AddAsync(employee);
                return result 
                    ? (true, $"Employee {employee.FullName} added successfully") 
                    : (false, "Failed to add employee to database");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding employee: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateEmployeeAsync(Models.Employee employee)
        {
            try
            {
                var validationResult = ValidateEmployee(employee);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.ErrorMessage);
                }

                var exists = await _repository.ExistsAsync(employee.EmployeeID);
                if (!exists)
                {
                    return (false, $"Employee with ID {employee.EmployeeID} not found");
                }

                var result = await _repository.UpdateAsync(employee);
                return result 
                    ? (true, $"Employee {employee.FullName} updated successfully") 
                    : (false, "Failed to update employee in database");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating employee: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteEmployeeAsync(string employeeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return (false, "Employee ID is required");
                }

                var result = await _repository.DeleteAsync(employeeId);
                return result 
                    ? (true, "Employee record deleted successfully") 
                    : (false, "Failed to delete employee");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting employee: {ex.Message}");
            }
        }

        public async Task<Models.Employee> GetEmployeeAsync(string employeeId)
        {
            return await _repository.GetByIdAsync(employeeId);
        }

        public async Task<IEnumerable<Models.Employee>> GetAllEmployeesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<string> GenerateNextEmployeeIdAsync()
        {
            return await _repository.GenerateNextEmployeeIdAsync();
        }

        public async Task<(bool Success, string Message)> TerminateEmployeeAsync(string employeeId, DateTime terminationDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId)) return (false, "Employee ID is required.");
                bool result = await _repository.TerminateAsync(employeeId, terminationDate);
                return result 
                    ? (true, "Employee contract terminated successfully.") 
                    : (false, "Failed to terminate employee record.");
            }
            catch (Exception ex)
            {
                return (false, "Error during termination: " + ex.Message);
            }
        }

        public async Task<DataTable> GetEmployeesTableAsync(string filterId = null, string filterDepartment = null)
        {
            return await _repository.GetAsTableAsync(filterId, filterDepartment);
        }

        public async Task<IEnumerable<string>> GetDepartmentsAsync()
        {
            var employees = await _repository.GetAllAsync();
            return employees
                .Select(e => e.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d);
        }

        private ValidationResult ValidateEmployee(Models.Employee employee)
        {
            if (employee == null)
                return new ValidationResult(false, "Employee data is required");

            if (!ValidationHelper.IsNotEmpty(employee.FullName))
                return new ValidationResult(false, "Full name is required");

            if (employee.DateOfBirth == default(DateTime))
                return new ValidationResult(false, "Date of birth is required");

            var age = employee.GetAge();
            if (age < 18)
                return new ValidationResult(false, "Employee must be at least 18 years old");

            if (!ValidationHelper.IsNotEmpty(employee.Contact))
                return new ValidationResult(false, "Contact number is required");

            if (employee.Salary <= 0)
                return new ValidationResult(false, "Salary must be a positive amount");

            return new ValidationResult(true, "");
        }

        private class ValidationResult
        {
            public bool IsValid { get; }
            public string ErrorMessage { get; }

            public ValidationResult(bool isValid, string errorMessage)
            {
                IsValid = isValid;
                ErrorMessage = errorMessage;
            }
        }
    }
}
