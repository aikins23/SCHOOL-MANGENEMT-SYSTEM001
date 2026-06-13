using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class CsvImportExportService
    {
        private readonly StudentService _studentService;
        private readonly IStudentRepository _studentRepository;

        public CsvImportExportService(StudentService studentService, IStudentRepository studentRepository)
        {
            _studentService = studentService;
            _studentRepository = studentRepository;
        }

        public async Task<(bool Success, string Message)> ExportStudentsToCsvAsync(string filePath)
        {
            try
            {
                var students = await _studentRepository.GetAllAsync();
                
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("StudentID,FirstName,LastName,DateOfBirth,Gender,ClassID,Email,HomeTown,Residence,Allergies,GuardianName,GuardianEmail,GuardianLocation,EmergencyContact");
                    
                    foreach (var s in students)
                    {
                        var line = string.Join(",", 
                            EscapeCsv(s.StudentID),
                            EscapeCsv(s.FirstName),
                            EscapeCsv(s.LastName),
                            EscapeCsv(s.DateOfBirth.ToString("yyyy-MM-dd")),
                            EscapeCsv(s.Gender),
                            EscapeCsv(s.ClassID),
                            EscapeCsv(s.Email),
                            EscapeCsv(s.HomeTown),
                            EscapeCsv(s.Residence),
                            EscapeCsv(s.Allergies),
                            EscapeCsv(s.GuardianName),
                            EscapeCsv(s.GuardianEmail),
                            EscapeCsv(s.GuardianLocation),
                            EscapeCsv(s.EmergencyContact)
                        );
                        writer.WriteLine(line);
                    }
                }
                
                return (true, $"Successfully exported {students.Count()} students.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Export to CSV failed", ex);
                return (false, "Export failed: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message, int Processed, int SentSms)> ImportStudentsFromCsvAsync(string filePath)
        {
            int processedCount = 0;
            int smsCount = 0;
            int newAutoCount = 0, newExplicitCount = 0, updatedCount = 0;
            var failures = new List<string>();

            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length <= 1) return (false, "The file is empty or only contains headers.", 0, 0);

                var existingStudents = await _studentRepository.GetAllAsync();
                var existingMap = existingStudents.ToDictionary(s => s.StudentID, StringComparer.OrdinalIgnoreCase);

                await AuthService.EnsureDatabaseSetupAsync();

                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var cols = ParseCsvLine(lines[i]);
                    if (cols.Count < 14)
                    {
                        failures.Add($"Line {i + 1}: expected 14 columns, found {cols.Count}.");
                        continue;
                    }

                    // Accept both numeric (9016) and display (KPS9016) ID forms.
                    string studentId = Common.StudentId.Parse(cols[0]);
                    bool isNew = false;
                    bool explicitNewId = false;

                    if (string.IsNullOrEmpty(studentId))
                    {
                        studentId = await _studentRepository.GenerateNextStudentIdAsync();
                        isNew = true;
                    }
                    else if (!existingMap.ContainsKey(studentId))
                    {
                        isNew = true;
                        explicitNewId = true;
                    }

                    Student student;
                    if (isNew)
                    {
                        student = new Student
                        {
                            StudentID = studentId,
                            AdmissionDate = DateTime.Now.Date,
                            ProfilePhoto = new byte[0]
                        };
                    }
                    else
                    {
                        var existing = existingMap[studentId];
                        student = new Student
                        {
                            StudentID = studentId,
                            AdmissionDate = existing.AdmissionDate != default ? existing.AdmissionDate : DateTime.Now.Date,
                            ProfilePhoto = existing.ProfilePhoto,
                            CreatedDate = existing.CreatedDate
                        };
                    }

                    student.FirstName = cols[1].Trim();
                    student.LastName = cols[2].Trim();
                    student.DateOfBirth = ParseDate(cols[3].Trim());
                    student.Gender = cols[4].Trim();
                    student.ClassID = cols[5].Trim();
                    student.Email = cols[6].Trim();
                    student.HomeTown = cols[7].Trim();
                    student.Residence = cols[8].Trim();
                    student.Allergies = cols[9].Trim();
                    student.GuardianName = cols[10].Trim();
                    student.GuardianEmail = cols[11].Trim();
                    student.GuardianLocation = cols[12].Trim();
                    student.EmergencyContact = cols[13].Trim(); // Guardian Phone Number

                    var res = isNew
                        ? await _studentService.AddStudentAsync(student)
                        : await _studentService.UpdateStudentAsync(student);

                    if (!res.Success)
                    {
                        failures.Add($"Line {i + 1} ({Common.StudentId.Display(studentId)}): {res.Message}");
                        continue;
                    }

                    processedCount++;
                    if (isNew && explicitNewId) newExplicitCount++;
                    else if (isNew) newAutoCount++;
                    else updatedCount++;

                    // One PARENT account per student; SMS the credentials to the guardian.
                    if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                    {
                        string username = Common.ImportCredentials.UsernameFor(student.StudentID);
                        string password = Common.ImportCredentials.NewPassword();

                        var authRes = await AuthService.RegisterAsync(username, password, password, "PARENT", null);
                        if (authRes.Success)
                        {
                            var smsRes = await SmsService.SendParentCredentialsAsync(
                                student.EmergencyContact, student.FullName, username, password);
                            if (smsRes.Success) smsCount++;
                            else failures.Add($"Line {i + 1} ({username}): SMS failed - {smsRes.Message}");
                        }
                        else if (authRes.Message != "Username already exists.")
                        {
                            // "already exists" = re-import; silently keep the original credentials.
                            failures.Add($"Line {i + 1} ({username}): account not created - {authRes.Message}");
                        }
                    }
                }

                foreach (var f in failures) LoggerHelper.LogWarning("Import: " + f);

                string summary =
                    $"Import completed. {newAutoCount} new (auto-ID), {newExplicitCount} new (explicit ID), " +
                    $"{updatedCount} updated, {failures.Count} failed. Sent {smsCount} SMS credentials.";
                if (failures.Count > 0)
                    summary += Environment.NewLine + Environment.NewLine + "Failures:" + Environment.NewLine +
                               string.Join(Environment.NewLine, failures.Take(10)) +
                               (failures.Count > 10 ? Environment.NewLine + $"... and {failures.Count - 10} more (see log)." : "");
                return (true, summary, processedCount, smsCount);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Import from CSV failed", ex);
                return (false, "Import failed: " + ex.Message, processedCount, smsCount);
            }
        }

        private static DateTime ParseDate(string value)
        {
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(value, formats, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var exact))
                return exact;
            return DateTime.TryParse(value, out var loose) ? loose : DateTime.Today.AddYears(-10);
        }

        private string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentField.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString());
            return result;
        }
    }
}
