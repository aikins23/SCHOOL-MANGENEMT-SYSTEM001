using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Examples
{
    /// <summary>
    /// EXAMPLE IMPLEMENTATION - Shows how to use modern architecture
    /// 
    /// This is a code reference demonstrating:
    /// - Dependency injection pattern
    /// - Service layer usage
    /// - Async/await pattern
    /// - Proper error handling
    /// - Form validation
    /// 
    /// To integrate this into actual forms, adapt the patterns shown here.
    /// This file is not a complete form implementation.
    /// </summary>
    public class StudentFormExample
    {
        private readonly StudentService _studentService;
        private Student _currentStudent;

        public StudentFormExample()
        {
            // Initialize service with repository
            var repository = new StudentRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(repository);
        }

        /// <summary>
        /// Example: Initialize form with data
        /// </summary>
        public async System.Threading.Tasks.Task InitializeFormAsync()
        {
            try
            {
                // Generate next student ID
                var nextId = await _studentService.GenerateNextStudentIdAsync();
                // Assign to txtStdID.Text in your form

                // Get all students
                var students = await _studentService.GetAllStudentsAsync();
                // Bind to DataGridView or ListBox

                MessageBox.Show($"Form initialized. Next student ID: {nextId}");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error initializing form: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Save new student
        /// </summary>
        public async System.Threading.Tasks.Task SaveStudentAsync(Student student)
        {
            try
            {
                var (success, message) = await _studentService.AddStudentAsync(student);

                if (success)
                {
                    UIHelper.ShowSuccess(message);
                    _currentStudent = null;
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error saving student: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Update existing student
        /// </summary>
        public async System.Threading.Tasks.Task UpdateStudentAsync(Student student)
        {
            if (_currentStudent == null)
            {
                UIHelper.ShowWarning("Please select a student to update");
                return;
            }

            try
            {
                student.StudentID = _currentStudent.StudentID;
                var (success, message) = await _studentService.UpdateStudentAsync(student);

                if (success)
                {
                    UIHelper.ShowSuccess(message);
                    _currentStudent = null;
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error updating student: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Delete student
        /// </summary>
        public async System.Threading.Tasks.Task DeleteStudentAsync(string studentId)
        {
            if (UIHelper.ShowConfirmation("Are you sure you want to delete this student?") != DialogResult.Yes)
                return;

            try
            {
                var (success, message) = await _studentService.DeleteStudentAsync(studentId);

                if (success)
                {
                    UIHelper.ShowSuccess(message);
                    _currentStudent = null;
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error deleting student: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Handle photo upload
        /// </summary>
        public byte[] HandlePhotoUpload()
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return ImageHelper.ConvertImageToBytes(openFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Error uploading photo: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Example: Create student model from form values
        /// </summary>
        public Student CreateStudentModel(
            string firstName, 
            string lastName,
            DateTime dateOfBirth,
            string gender,
            string classId,
            string email,
            string homeTown,
            string residence,
            string allergies,
            byte[] photo,
            string guardianName,
            string guardianEmail,
            string guardianLocation,
            string emergencyContact,
            DateTime admissionDate)
        {
            return new Student
            {
                StudentID = Guid.NewGuid().ToString().Substring(0, 8),
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                ClassID = classId,
                Email = email,
                HomeTown = homeTown,
                Residence = residence,
                Allergies = allergies,
                ProfilePhoto = photo,
                GuardianName = guardianName,
                GuardianEmail = guardianEmail,
                GuardianLocation = guardianLocation,
                EmergencyContact = emergencyContact,
                AdmissionDate = admissionDate
            };
        }

        /// <summary>
        /// Example: Validate form input
        /// </summary>
        public bool ValidateStudentInput(string firstName, string lastName, DateTime dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                UIHelper.ShowError("First name is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                UIHelper.ShowError("Last name is required");
                return false;
            }

            if (dateOfBirth >= DateTime.Today)
            {
                UIHelper.ShowError("Date of birth must be in the past");
                return false;
            }

            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth > DateTime.Today.AddYears(-age)) age--;

            if (age < AppConfig.MinStudentAge || age > AppConfig.MaxStudentAge)
            {
                UIHelper.ShowError($"Student age must be between {AppConfig.MinStudentAge} and {AppConfig.MaxStudentAge} years");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Example: Get configuration values
        /// </summary>
        public void ShowConfigurationValues()
        {
            var message = $@"
Configuration Values:
- Connection String: {AppConfig.ConnectionString}
- Min Age: {AppConfig.MinStudentAge}
- Max Age: {AppConfig.MaxStudentAge}
- Max Photo Size: {AppConfig.MaxPhotoSizeMB}MB
- Class Count: {AppConfig.ClassNames.Length}
";
            MessageBox.Show(message);
        }
    }
}
