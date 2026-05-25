# Student Terminal Report Card Printing System - Design Specification

> **For agentic workers:** Use superpowers:writing-plans to create the implementation plan for this design.

**Goal:** Create a professional student terminal report card printing system that generates PDF reports matching the Kingdom Preparatory School template, with support for single-student and batch printing to both digital and physical printers.

**Architecture:** Layered service architecture with separated concerns—ReportCardDataService (data retrieval), ReportCardPDFGenerator (PDF creation), ReportCardPrinter (output handling), and ReportCardManager (orchestration).

**Tech Stack:** C# .NET 4.7.2, PDFsharp 6.1.0, WinForms, OleDb data access, async/await patterns.

---

## 1. Database Schema

### New Table: StudentTermRemarks
Stores teacher remarks and behavioral observations per student per term.

```sql
CREATE TABLE StudentTermRemarks (
    ID INT PRIMARY KEY IDENTITY(1,1),
    StudentID NVARCHAR(50) NOT NULL FOREIGN KEY REFERENCES Student(StudentID),
    Term NVARCHAR(50) NOT NULL,        -- "TERM 1", "TERM 2", "TERM 3"
    Year NVARCHAR(20) NOT NULL,        -- "2024/2025"
    ClassTeacherRemarks NVARCHAR(MAX),
    HeadTeacherRemarks NVARCHAR(MAX),
    Attitude NVARCHAR(500),
    Interest NVARCHAR(500),
    Conduct NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME,
    UNIQUE(StudentID, Term, Year)
);
```

### Existing Tables Used
- **Student** — StudentID, FirstName, LastName, ClassID, Gender, ProfilePhoto, AdmissionDate
- **ExamResult** — StudentId, ClassId, Subject, Term, Year, Category1 (Test), Category2 (Group), Category3 (Project), ExamScore, TotalScore, Grade, Remark
- **Attendance** — StudentID, AttendanceDate, Status (Present/Absent/Leave)
- **ClassConfig** — ClassName, PromotionLevel, TuitionFee

---

## 2. Data Model

### ReportCardData (DTO - Data Transfer Object)

Encapsulates all data needed to generate a single report card.

```csharp
public class ReportCardData
{
    // Student Information
    public string StudentID { get; set; }
    public string StudentName { get; set; }
    public string ClassID { get; set; }
    public string Gender { get; set; }
    public byte[] ProfilePhoto { get; set; }
    public DateTime AdmissionDate { get; set; }
    
    // Academic Information
    public string Term { get; set; }
    public string Year { get; set; }
    
    // Attendance Summary
    public int PresentDays { get; set; }
    public int TotalSchoolDays { get; set; }
    public int AttendancePercentage => (PresentDays * 100) / TotalSchoolDays;
    
    // Subject Results with Rankings
    public List<SubjectResult> SubjectResults { get; set; }
    
    // Overall Rankings
    public int OverallPosition { get; set; }
    public int TotalStudentsInClass { get; set; }
    
    // Remarks
    public StudentTermRemarks Remarks { get; set; }
    
    // School Information (hardcoded for now)
    public SchoolInfo SchoolInfo { get; set; }
}

public class SubjectResult
{
    public string Subject { get; set; }
    public decimal ClassScore { get; set; }        // 0-60 (Test + Group + Project)
    public decimal ExamScore { get; set; }         // 0-100
    public decimal TotalScore { get; set; }        // Calculated: (ClassScore/60)*50 + (ExamScore/100)*50
    public string Grade { get; set; }              // "1", "2", "3", "4", "5"
    public string Remark { get; set; }             // "Advanced", "Proficiency", etc.
    public int PositionInClass { get; set; }       // Ranked against peers in same class/term/year/subject
}

public class SchoolInfo
{
    public string Name { get; set; } = "KINGDOM PREPARATORY SCHOOL";
    public string Location { get; set; } = "AKIM ODA- ABENASE";
    public string PhoneNumbers { get; set; } = "0548050141/0246087609";
    public byte[] Logo { get; set; }               // School logo image
}
```

---

## 3. Service Architecture

### 3.1 ReportCardDataService

**Responsibility:** Retrieve and aggregate all data needed for report card generation. Calculate rankings, attendance summaries, and prepare data for PDF rendering.

**Key Methods:**

```csharp
public class ReportCardDataService
{
    private readonly IExamResultRepository _examRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IStudentTermRemarksRepository _remarksRepository;
    
    /// <summary>
    /// Retrieves complete report card data for a single student.
    /// </summary>
    public async Task<ReportCardData> GetStudentReportCardDataAsync(
        string studentId, 
        string term, 
        string year)
    {
        // 1. Get student info
        var student = await _studentRepository.GetByIdAsync(studentId);
        
        // 2. Get all subjects/exams for this student in this term/year
        var examResults = await _examRepository.GetByStudentTermYearAsync(studentId, term, year);
        
        // 3. Calculate subject-level rankings:
        //    For each subject, rank this student vs all students in same class/term/year
        var subjectRankings = await CalculateSubjectRankingsAsync(student.ClassID, examResults, term, year);
        
        // 4. Calculate overall ranking:
        //    Rank student by sum of all subject TotalScores vs all students in class
        var overallRanking = await CalculateOverallRankingAsync(student.ClassID, studentId, term, year);
        
        // 5. Get attendance summary
        var attendanceSummary = await _attendanceRepository.GetSummaryAsync(studentId, term, year);
        
        // 6. Get teacher remarks
        var remarks = await _remarksRepository.GetAsync(studentId, term, year);
        
        // 7. Aggregate into ReportCardData DTO
        return new ReportCardData
        {
            StudentID = student.StudentID,
            StudentName = student.FullName,
            ClassID = student.ClassID,
            Gender = student.Gender,
            ProfilePhoto = student.ProfilePhoto,
            AdmissionDate = student.AdmissionDate,
            Term = term,
            Year = year,
            PresentDays = attendanceSummary.PresentDays,
            TotalSchoolDays = attendanceSummary.TotalDays,
            SubjectResults = subjectRankings,
            OverallPosition = overallRanking.Position,
            TotalStudentsInClass = overallRanking.TotalStudents,
            Remarks = remarks,
            SchoolInfo = new SchoolInfo()
        };
    }
    
    /// <summary>
    /// Calculates position in class for each subject.
    /// Ranking is by TotalScore (highest = position 1).
    /// </summary>
    private async Task<List<SubjectResult>> CalculateSubjectRankingsAsync(
        string classId, 
        List<ExamResult> studentResults, 
        string term, 
        string year)
    {
        var results = new List<SubjectResult>();
        
        foreach (var exam in studentResults)
        {
            // Get all students' scores for this subject/class/term/year
            var allClassResults = await _examRepository.GetBySubjectClassTermYearAsync(
                exam.Subject, classId, term, year);
            
            // Rank: count how many students scored higher
            var position = allClassResults.Count(x => x.TotalScore > exam.TotalScore) + 1;
            
            results.Add(new SubjectResult
            {
                Subject = exam.Subject,
                ClassScore = exam.Category1 + exam.Category2 + exam.Category3,
                ExamScore = exam.ExamScore,
                TotalScore = exam.TotalScore,
                Grade = exam.Grade,
                Remark = exam.Remark,
                PositionInClass = position
            });
        }
        
        return results;
    }
    
    /// <summary>
    /// Calculates overall position in class.
    /// Ranking is by sum of all subject TotalScores (highest = position 1).
    /// </summary>
    private async Task<(int Position, int TotalStudents)> CalculateOverallRankingAsync(
        string classId, 
        string studentId, 
        string term, 
        string year)
    {
        // Get all students in this class with their total aggregate scores
        var allClassStudents = await _examRepository.GetAllStudentsAggregateScoresAsync(
            classId, term, year);
        
        // Calculate this student's aggregate score
        var studentAggregate = allClassStudents
            .FirstOrDefault(x => x.StudentID == studentId);
        
        // Rank: count how many students have higher aggregate
        var position = allClassStudents.Count(x => x.AggregateScore > studentAggregate.AggregateScore) + 1;
        
        return (position, allClassStudents.Count);
    }
}
```

### 3.2 ReportCardPDFGenerator

**Responsibility:** Take ReportCardData and generate a professional PDF matching the Kingdom Preparatory School template layout.

**Key Methods:**

```csharp
public class ReportCardPDFGenerator
{
    /// <summary>
    /// Generates a complete report card PDF as a byte array.
    /// </summary>
    public async Task<byte[]> GeneratePDFAsync(ReportCardData data)
    {
        using (var document = new PdfDocument())
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            
            // 1. Draw header section (dark blue banner)
            DrawHeader(gfx, page, data.SchoolInfo, data.ProfilePhoto);
            
            // 2. Draw student info section
            DrawStudentInfo(gfx, page, data);
            
            // 3. Draw subjects table + grading legend
            DrawSubjectsTableAndLegend(gfx, page, data.SubjectResults);
            
            // 4. Draw remarks section
            DrawRemarksSection(gfx, page, data.Remarks);
            
            // 5. Draw signature section
            DrawSignatureSection(gfx, page);
            
            // Convert to byte array
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return stream.ToArray();
            }
        }
    }
    
    // Private drawing methods for each section
    private void DrawHeader(XGraphics gfx, PdfPage page, SchoolInfo schoolInfo, byte[] photo) { /* ... */ }
    private void DrawStudentInfo(XGraphics gfx, PdfPage page, ReportCardData data) { /* ... */ }
    private void DrawSubjectsTableAndLegend(XGraphics gfx, PdfPage page, List<SubjectResult> results) { /* ... */ }
    private void DrawRemarksSection(XGraphics gfx, PdfPage page, StudentTermRemarks remarks) { /* ... */ }
    private void DrawSignatureSection(XGraphics gfx, PdfPage page) { /* ... */ }
}
```

### 3.3 ReportCardPrinter

**Responsibility:** Handle output to physical printer or save to file.

**Key Methods:**

```csharp
public class ReportCardPrinter
{
    /// <summary>
    /// Sends PDF to physical printer.
    /// </summary>
    public async Task PrintToPrinterAsync(byte[] pdfBytes, string printerName = null)
    {
        // If no printer specified, use default
        if (string.IsNullOrEmpty(printerName))
            printerName = GetDefaultPrinterName();
        
        // Save temporarily, print via Windows printing API, clean up
        var tempPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempPath, pdfBytes);
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tempPath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
    
    /// <summary>
    /// Saves PDF to file system.
    /// </summary>
    public async Task SaveToFileAsync(byte[] pdfBytes, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        await File.WriteAllBytesAsync(filePath, pdfBytes);
    }
    
    /// <summary>
    /// Shows print dialog, allows user to choose printer/settings.
    /// </summary>
    public PrintDialog ShowPrintDialog()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
            return dialog;
        return null;
    }
    
    private string GetDefaultPrinterName()
    {
        // Get default printer from Windows
        var settings = new PrinterSettings();
        return settings.PrinterName;
    }
}
```

### 3.4 ReportCardManager

**Responsibility:** Orchestrate the complete workflow—coordinate data retrieval, PDF generation, and output.

**Key Methods:**

```csharp
public class ReportCardManager
{
    private readonly ReportCardDataService _dataService;
    private readonly ReportCardPDFGenerator _pdfGenerator;
    private readonly ReportCardPrinter _printer;
    
    /// <summary>
    /// Main entry point: Generate and output single student report card.
    /// </summary>
    public async Task GenerateAndOutputAsync(
        string studentId, 
        string term, 
        string year, 
        ReportCardOutputAction action)
    {
        try
        {
            // 1. Retrieve all necessary data
            var reportData = await _dataService.GetStudentReportCardDataAsync(studentId, term, year);
            
            // 2. Generate PDF
            var pdfBytes = await _pdfGenerator.GeneratePDFAsync(reportData);
            
            // 3. Output based on action
            switch (action.Type)
            {
                case OutputType.Print:
                    await _printer.PrintToPrinterAsync(pdfBytes, action.PrinterName);
                    break;
                    
                case OutputType.Save:
                    var fileName = $"{studentId}_{reportData.StudentName.Replace(" ", "_")}_ReportCard.pdf";
                    var filePath = Path.Combine(action.SavePath, fileName);
                    await _printer.SaveToFileAsync(pdfBytes, filePath);
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new ReportCardGenerationException($"Failed to generate report card for {studentId}", ex);
        }
    }
    
    /// <summary>
    /// Generate batch report cards for multiple students.
    /// </summary>
    public async Task GenerateBatchAsync(
        List<string> studentIds, 
        string term, 
        string year, 
        string savePath,
        IProgress<BatchProgressReport> progress = null)
    {
        int processed = 0;
        var total = studentIds.Count;
        
        foreach (var studentId in studentIds)
        {
            try
            {
                var reportData = await _dataService.GetStudentReportCardDataAsync(studentId, term, year);
                var pdfBytes = await _pdfGenerator.GeneratePDFAsync(reportData);
                
                var fileName = $"{studentId}_{reportData.StudentName.Replace(" ", "_")}_ReportCard.pdf";
                var filePath = Path.Combine(savePath, fileName);
                await _printer.SaveToFileAsync(pdfBytes, filePath);
                
                processed++;
                progress?.Report(new BatchProgressReport { Current = processed, Total = total });
            }
            catch (Exception ex)
            {
                // Log error but continue processing other students
                Debug.WriteLine($"Error generating report card for {studentId}: {ex.Message}");
            }
        }
    }
}

public enum OutputType { Print, Save }
public class ReportCardOutputAction
{
    public OutputType Type { get; set; }
    public string PrinterName { get; set; }      // For Print action
    public string SavePath { get; set; }         // For Save action
}

public class BatchProgressReport
{
    public int Current { get; set; }
    public int Total { get; set; }
    public int Percentage => (Current * 100) / Total;
}
```

---

## 4. UI Integration

### 4.1 EXAMSVIEW Single Report Card
Add button in exam results grid row actions:
- User clicks student row → Detail panel shows
- "Print Report Card" button → Shows output dialog → Generates and outputs

### 4.2 New Form: GenerateReportCards
New dedicated form for batch generation:
- Filter options: Class (dropdown), Term (dropdown), Year (dropdown)
- Checkbox: "Print All Students (Entire School)"
- Button: "Generate"
- Progress indicator during batch processing
- Output options: "Print All" or "Save to Folder"

---

## 5. PDF Layout Specification

**Page Setup:** A4 (210×297mm), 10mm margins, White background

**Section 1: Header (120mm height)**
- Background: Dark Navy Blue (#1A2B47)
- Layout: 3 columns (20% | 60% | 20%)
  - Left: School logo (100×100mm)
  - Center: School name (Segoe UI Bold, 28pt, White) + Contact info (Segoe UI, 11pt, White)
  - Right: Student photo (100×120mm)

**Section 2: Student Info (50mm height)**
- Background: Light gray (#F0F0F0)
- 2 columns × 3 rows table:
  - Row 1: Student Name | Resuming Date
  - Row 2: Admission No. | Attendance
  - Row 3: Class/Form | Number On Roll

**Section 3: Subjects Table**
- Columns: Subject | Class Score (50%) | Exams Score (60%) | Total Score (100%) | Grade | Position Per Subject | Remarks
- Font: Segoe UI, 9pt
- Row height: 12mm per subject
- Alternating row backgrounds for readability

**Section 4: Grading Legend (right side of subjects table)**
- Score | Grade | Remarks
- 80+ | 1 | Advanced(A)
- 75-79 | 2 | Proficiency(P)
- 70-74 | 3 | Approaching Proficiency(AP)
- 65-69 | 4 | Developing
- <65 | 5 | Beginning

**Section 5: Remarks (40mm height)**
- Rows: Attitude | Interest | Conduct | Class Teacher's Remarks | Head Teacher's Remarks
- Empty space for teacher annotations

**Section 6: Signature Section**
- 2 columns
- Left: "School Director's Signature _____________ Signature & Stamp"
- Right: "Head Teacher's Signature & Stamp _____________"

---

## 6. File Structure

```
Services/
├── ReportCardDataService.cs
├── ReportCardPDFGenerator.cs
├── ReportCardPrinter.cs
└── ReportCardManager.cs

Models/
├── ReportCardData.cs
├── SubjectResult.cs
├── SchoolInfo.cs
├── StudentTermRemarks.cs (add to existing)
└── ReportCardOutputAction.cs

Data/
├── IStudentTermRemarksRepository.cs
├── StudentTermRemarksRepository.cs
└── (extend existing repositories if needed)

Forms/
├── GenerateReportCardsForm.cs
└── GenerateReportCardsForm.Designer.cs
```

---

## 7. Error Handling

- **DataRetrievalException:** When student/exam/attendance data cannot be retrieved
- **PDFGenerationException:** When PDF rendering fails (missing fonts, layout issues)
- **PrintingException:** When printer communication fails
- **ReportCardGenerationException:** Wrapper for all report card-related errors

All exceptions logged and user-friendly messages shown in UI.

---

## 8. Testing Strategy

- **Unit Tests:** Data aggregation logic, ranking calculations, PDF content generation
- **Integration Tests:** Full workflow (data → PDF → file)
- **Manual Testing:** PDF visual inspection, printer output verification, batch performance

---

## 9. Future Enhancements

- SchoolInfo table-driven (currently hardcoded)
- School logo upload/management UI
- PDF template customization (school-specific layouts)
- Email delivery of report cards
- Digital signature capture for teacher remarks
- Multi-language support

---

**Status:** Ready for implementation planning
