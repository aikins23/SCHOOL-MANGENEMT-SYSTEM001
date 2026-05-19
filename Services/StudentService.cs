using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Business logic service for student operations with centralized validation and error handling
    /// </summary>
    public class StudentService
    {
        private readonly Data.IStudentRepository _repository;

        public StudentService(Data.IStudentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Adds a new student after validation
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

                // Check if student already exists
                if (await _repository.ExistsAsync(student.StudentID))
                {
                    return (false, "Student with this ID already exists");
                }

                // Generate ID if not provided
                if (string.IsNullOrWhiteSpace(student.StudentID))
                {
                    student.StudentID = await _repository.GenerateNextStudentIdAsync();
                }

                var result = await _repository.AddAsync(student);
                return result 
                    ? (true, $"Student {student.FullName} added successfully") 
                    : (false, "Failed to add student");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding student: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing student
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
                return result 
                    ? (true, $"Student {student.FullName} updated successfully") 
                    : (false, "Failed to update student");
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
            if (age < 2 || age > 25)
                return new ValidationResult(false, "Student age must be between 2 and 25 years");

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
