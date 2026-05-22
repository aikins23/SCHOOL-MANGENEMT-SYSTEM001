using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Business logic service for student operations with centralized validation and error handling
    /// </summary>
    public class StudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IFeeRepository _feeRepository;

        public StudentService(IStudentRepository repository, IFeeRepository feeRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _feeRepository = feeRepository ?? throw new ArgumentNullException(nameof(feeRepository));
        }

        /// <summary>
        /// Adds a new student and their initial fee records after validation
        /// </summary>
        public async Task<(bool Success, string Message)> AddStudentAsync(Models.Student student)
        {
            try
            {
                // Validate student data
                var validationResult = ValidateStudent(student);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.ErrorMessage);
                }

                // Check if student already exists (if ID provided)
                if (!string.IsNullOrWhiteSpace(student.StudentID) && await _repository.ExistsAsync(student.StudentID))
                {
                    return (false, "Student with this ID already exists");
                }

                // Add student to database
                var result = await _repository.AddAsync(student);
                if (!result)
                {
                    return (false, "Failed to add student to database");
                }

                // Handle initial fee records
                decimal fee = GetFeeForClass(student.ClassID);
                await _feeRepository.AddInitialFeeRecordAsync(student.StudentID, student.ClassID, fee);
                await _feeRepository.AddInitialPaymentRecordAsync(student.StudentID, student.ClassID, student.FullName, fee);

                return (true, $"Student {student.FullName} added successfully with opening fee record");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding student: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing student and their fee records
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateStudentAsync(Models.Student student)
        {
            try
            {
                var validationResult = ValidateStudent(student);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.ErrorMessage);
                }

                var exists = await _repository.ExistsAsync(student.StudentID);
                if (!exists)
                {
                    return (false, $"Student with ID {student.StudentID} not found");
                }

                var result = await _repository.UpdateAsync(student);
                if (!result)
                {
                    return (false, "Failed to update student in database");
                }

                // Update fee records
                decimal fee = GetFeeForClass(student.ClassID);
                await _feeRepository.UpdateFeeRecordAsync(student.StudentID, student.ClassID, fee);
                await _feeRepository.UpdatePaymentRecordAsync(student.StudentID, student.ClassID, student.FullName, fee);

                return (true, $"Student {student.FullName} updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating student: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a student
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteStudentAsync(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return (false, "Student ID is required");
                }

                var result = await _repository.DeleteAsync(studentId);
                return result 
                    ? (true, "Student deleted successfully") 
                    : (false, "Failed to delete student");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting student: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a student by ID
        /// </summary>
        public async Task<Models.Student> GetStudentAsync(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                throw new ArgumentException("Student ID is required", nameof(studentId));

            return await _repository.GetByIdAsync(studentId);
        }

        /// <summary>
        /// Gets all students
        /// </summary>
        public async Task<IEnumerable<Models.Student>> GetAllStudentsAsync()
        {
            return await _repository.GetAllAsync();
        }

        /// <summary>
        /// Gets students by class
        /// </summary>
        public async Task<IEnumerable<Models.Student>> GetStudentsByClassAsync(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
                throw new ArgumentException("Class ID is required", nameof(classId));

            return await _repository.GetByClassAsync(classId);
        }

        /// <summary>
        /// Generates next student ID
        /// </summary>
        public async Task<string> GenerateNextStudentIdAsync()
        {
            return await _repository.GenerateNextStudentIdAsync();
        }

        public async Task<DataTable> GetStudentsTableAsync(string filterId = null, string filterClass = null)
        {
            return await _repository.GetAsTableAsync(filterId, filterClass);
        }

        public async Task<DataTable> GetRolledOutStudentsTableAsync()
        {
            return await _repository.GetRolledOutAsTableAsync();
        }

        public async Task<(bool Success, string Message)> PromoteStudentsAsync(IEnumerable<string> studentIds, string targetClassId)
        {
            if (studentIds == null || !studentIds.Any()) return (false, "No students selected for promotion.");
            if (string.IsNullOrWhiteSpace(targetClassId)) return (false, "Target class is required.");

            try
            {
                bool result = await _repository.UpdateStudentClassBatchAsync(studentIds, targetClassId);
                return result 
                    ? (true, $"{studentIds.Count()} student(s) promoted to {targetClassId} successfully.") 
                    : (false, "Failed to promote students.");
            }
            catch (Exception ex)
            {
                return (false, "Error during promotion: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> RollOutStudentAsync(string studentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId)) return (false, "Student ID is required.");
                bool result = await _repository.RollOutAsync(studentId);
                return result 
                    ? (true, "Student rolled out successfully.") 
                    : (false, "Failed to roll out student.");
            }
            catch (Exception ex)
            {
                return (false, "Error during roll out: " + ex.Message);
            }
        }

        /// <summary>
        /// Calculates the fee based on class
        /// </summary>
        public decimal GetFeeForClass(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) return 1200m;

            switch (classId.Trim().ToUpperInvariant())
            {
                case "CRECHE":
                    return 2000m;
                case "NURSERY 1":
                    return 3450m;
                case "NURSERY 2":
                    return 3750m;
                case "KINDERGARTEN 1":
                    return 3654m;
                case "KINDERGARTEN 2":
                case "BASIC 1":
                case "BASIC 2":
                case "BASIC 3":
                case "BASIC 4":
                case "BASIC 5":
                case "BASIC 6":
                case "BASIC 7":
                case "BASIC 8":
                    return 2423m;
                default:
                    return 1200m;
            }
        }

        /// <summary>
        /// Validates student data using centralized ValidationHelper
        /// </summary>
        private ValidationResult ValidateStudent(Models.Student student)
        {
            if (student == null)
                return new ValidationResult(false, "Student data is required");

            if (!ValidationHelper.IsNotEmpty(student.FirstName))
                return new ValidationResult(false, "First name is required");

            if (!ValidationHelper.IsValidName(student.FirstName))
                return new ValidationResult(false, "First name contains invalid characters");

            if (!ValidationHelper.IsNotEmpty(student.LastName))
                return new ValidationResult(false, "Last name is required");

            if (!ValidationHelper.IsValidName(student.LastName))
                return new ValidationResult(false, "Last name contains invalid characters");

            if (student.DateOfBirth == default(DateTime))
                return new ValidationResult(false, "Date of birth is required");

            if (!ValidationHelper.IsNotFutureDate(student.DateOfBirth.ToString()))
                return new ValidationResult(false, "Date of birth must be in the past");

            var age = student.GetAge();
            if (age < AppConfig.MinStudentAge || age > AppConfig.MaxStudentAge)
                return new ValidationResult(false, $"Student age must be between {AppConfig.MinStudentAge} and {AppConfig.MaxStudentAge} years");

            if (!ValidationHelper.IsNotEmpty(student.Gender))
                return new ValidationResult(false, "Gender is required");

            if (!ValidationHelper.IsNotEmpty(student.ClassID))
                return new ValidationResult(false, "Class is required");

            if (!string.IsNullOrWhiteSpace(student.Email) && !ValidationHelper.IsValidEmail(student.Email))
                return new ValidationResult(false, "Email format is invalid");

            if (!string.IsNullOrWhiteSpace(student.HomeTown) && student.HomeTown.Length > 100)
                return new ValidationResult(false, "Home town must not exceed 100 characters");

            if (student.AdmissionDate == default(DateTime))
                return new ValidationResult(false, "Admission date is required");

            if (!ValidationHelper.IsNotFutureDate(student.AdmissionDate.ToString()))
                return new ValidationResult(false, "Admission date must not be in the future");

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
