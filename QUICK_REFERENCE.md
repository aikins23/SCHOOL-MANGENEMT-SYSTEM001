# Quick Reference - Modern Architecture

## 🎯 Quick Start Template

```csharp
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Common;

public partial class MyForm : Form
{
    private readonly StudentService _studentService;

    public MyForm()
    {
        InitializeComponent();
        var repository = new StudentRepository(AppConfig.ConnectionString);
        _studentService = new StudentService(repository);
    }

    private async void LoadData()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            // Use students
        }
        catch (Exception ex)
        {
            UIHelper.ShowError($"Error: {ex.Message}");
        }
    }

    private async void SaveStudent()
    {
        try
        {
            var student = new Models.Student
            {
                FirstName = txtFirstName.Text,
                LastName = txtLastName.Text,
                // ... other properties
            };

            var (success, message) = await _studentService.AddStudentAsync(student);

            if (success)
                UIHelper.ShowSuccess(message);
            else
                UIHelper.ShowError(message);
        }
        catch (Exception ex)
        {
            UIHelper.ShowError($"Error: {ex.Message}");
        }
    }
}
```

---

## 🔧 Common Tasks

### Add a New Student
```csharp
var (success, message) = await _studentService.AddStudentAsync(student);
```

### Update a Student
```csharp
var (success, message) = await _studentService.UpdateStudentAsync(student);
```

### Delete a Student
```csharp
var (success, message) = await _studentService.DeleteStudentAsync(studentId);
```

### Get Single Student
```csharp
var student = await _studentService.GetStudentAsync(studentId);
```

### Get All Students
```csharp
var students = await _studentService.GetAllStudentsAsync();
```

### Get Students by Class
```csharp
var students = await _studentService.GetStudentsByClassAsync(classId);
```

### Generate Next ID
```csharp
var nextId = await _studentService.GenerateNextStudentIdAsync();
```

---

## 🎨 UI Helpers

### Show Messages
```csharp
UIHelper.ShowSuccess("Operation completed!");
UIHelper.ShowError("Something went wrong");
UIHelper.ShowWarning("Please check this");
var result = UIHelper.ShowConfirmation("Continue?");
```

### Validation Feedback
```csharp
UIHelper.SetControlError(textBox, "This field is required");
UIHelper.ClearFormErrors(this);
```

---

## 📸 Image Handling

### Upload Photo
```csharp
var imageBytes = ImageHelper.ConvertImageToBytes(filePath);
student.ProfilePhoto = imageBytes;
```

### Display Photo
```csharp
var image = ImageHelper.BytesToImage(student.ProfilePhoto);
pictureBox.Image = image;
```

---

## ⚙️ Configuration

### Access Settings
```csharp
string connString = AppConfig.ConnectionString;
string[] classes = AppConfig.ClassNames;
string[] genders = AppConfig.GenderOptions;
int minAge = AppConfig.MinStudentAge;
int maxAge = AppConfig.MaxStudentAge;

// UI Colors
var color = AppConfig.Colors.PrimaryColor;
```

---

## ❌ Error Handling Pattern

```csharp
try
{
    var result = await _studentService.AddStudentAsync(student);
    if (result.Success)
    {
        UIHelper.ShowSuccess(result.Message);
    }
    else
    {
        UIHelper.ShowError(result.Message);
    }
}
catch (DataException ex)
{
    UIHelper.ShowError($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    UIHelper.ShowError($"Unexpected error: {ex.Message}");
}
```

---

## 📋 Validation

### Automatic Validations (In StudentService)
- ✓ Required fields
- ✓ Email format
- ✓ Age range (2-25)
- ✓ Date of birth in past
- ✓ Unique student ID
- ✓ Null checks

### Manual Validation (In Forms)
```csharp
if (string.IsNullOrWhiteSpace(txtFirstName.Text))
{
    UIHelper.ShowError("First name is required");
    return;
}
```

---

## 🔄 Return Format

All service methods return status tuples:
```csharp
(bool Success, string Message)
```

Example:
```csharp
var (success, message) = await _studentService.AddStudentAsync(student);
// success: true/false
// message: "Student added successfully" or error description
```

---

## 📚 Properties of Student Model

```csharp
student.StudentID              // Unique identifier
student.FirstName              // First name
student.LastName               // Last name
student.FullName               // Read-only: "{FirstName} {LastName}"
student.DateOfBirth            // Birth date
student.Gender                 // "MALE" or "FEMALE"
student.ClassID                // Class name
student.Email                  // Email address
student.HomeTown               // Home town
student.Residence              // Residence
student.Allergies              // Allergies info
student.ProfilePhoto           // Photo as byte[]
student.GuardianName           // Guardian name
student.GuardianEmail          // Guardian email
student.GuardianLocation       // Guardian location
student.EmergencyContact       // Emergency contact number
student.AdmissionDate          // Admission date
student.CreatedDate            // Record creation date
student.ModifiedDate           // Last modification date
student.GetAge()               // Method: calculated age
```

---

## 🧪 Testing Example

```csharp
[TestClass]
public class StudentServiceTests
{
    private IStudentRepository _repository;
    private StudentService _service;

    [TestInitialize]
    public void Setup()
    {
        _repository = new StudentRepository(AppConfig.ConnectionString);
        _service = new StudentService(_repository);
    }

    [TestMethod]
    public async Task AddStudent_WithValidData_ReturnsSuccess()
    {
        var student = new Models.Student
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddYears(-10),
            Gender = "MALE",
            ClassID = "BASIC 1",
            AdmissionDate = DateTime.Today
        };

        var (success, message) = await _service.AddStudentAsync(student);

        Assert.IsTrue(success);
        Assert.IsTrue(message.Contains("successfully"));
    }
}
```

---

## 🚀 Performance Tips

1. **Use async/await** to avoid blocking UI
2. **Cache frequently accessed data** like class lists
3. **Use GetByClassAsync** instead of filtering in-memory
4. **Batch operations** when possible
5. **Dispose database connections** properly (handled by repository)

---

## 🔐 Security Considerations

1. **Parameterized Queries** - Already implemented in repository
2. **Input Validation** - In StudentService
3. **SQL Injection Protection** - Using OleDbParameter
4. **Sensitive Data** - Store photos as bytes, not paths

---

## 📝 Logging (Future Enhancement)

```csharp
// When logging is added:
Logger.Info($"Added student: {student.StudentID}");
Logger.Error($"Failed to add student: {ex.Message}");
Logger.Debug("Database operation completed");
```

---

## 🎯 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "ConnectionString is null" | Check App.config settings |
| "Validation failed" | Check returned message for details |
| "UI freezes" | Ensure using async/await |
| "Parameter error" | Check Student property names match DB columns |
| "Image not showing" | Verify ProfilePhoto is not null |

---

## 📞 Getting Help

1. Review `MODERNIZATION_GUIDE.md` for detailed documentation
2. Check `Examples/frmAddStdModern.cs` for implementation example
3. Look at unit tests for usage patterns
4. Console output shows validation error messages
5. Check App.config for connection string issues

---

**Last Updated:** December 2024
**Version:** 1.0
