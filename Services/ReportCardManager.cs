using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Orchestrates the complete report card workflow
    /// Coordinates data retrieval, PDF generation, and output
    /// </summary>
    public class ReportCardManager
    {
        private readonly ReportCardDataService _dataService;
        private readonly ReportCardPDFGenerator _pdfGenerator;
        private readonly ReportCardPrinter _printer;

        public ReportCardManager(
            ReportCardDataService dataService,
            ReportCardPDFGenerator pdfGenerator,
            ReportCardPrinter printer)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
            _printer = printer ?? throw new ArgumentNullException(nameof(printer));
        }

        /// <summary>
        /// Main entry point: Generate and output single student report card
        /// </summary>
        public async Task GenerateAndOutputAsync(
            string studentId,
            string term,
            string year,
            ReportCardOutputAction action)
        {
            if (string.IsNullOrEmpty(studentId))
                throw new ArgumentException("Student ID cannot be empty", nameof(studentId));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

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
                        if (string.IsNullOrEmpty(action.SavePath))
                            throw new InvalidOperationException("Save path must be specified for Save action");

                        var fileName = $"{studentId}_{reportData.StudentName.Replace(" ", "_")}_ReportCard.pdf";
                        var filePath = System.IO.Path.Combine(action.SavePath, fileName);
                        await _printer.SaveToFileAsync(pdfBytes, filePath);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown output type: {action.Type}");
                }
            }
            catch (Exception ex)
            {
                throw new ReportCardGenerationException(
                    $"Failed to generate report card for student {studentId}", ex);
            }
        }

        /// <summary>
        /// Generate batch report cards for multiple students
        /// </summary>
        public async Task GenerateBatchAsync(
            List<string> studentIds,
            string term,
            string year,
            string savePath,
            IProgress<BatchProgressReport> progress = null)
        {
            if (studentIds == null || studentIds.Count == 0)
                throw new ArgumentException("Student list cannot be empty", nameof(studentIds));
            if (string.IsNullOrEmpty(savePath))
                throw new ArgumentException("Save path must be specified", nameof(savePath));

            int processed = 0;
            var total = studentIds.Count;

            foreach (var studentId in studentIds)
            {
                try
                {
                    var reportData = await _dataService.GetStudentReportCardDataAsync(
                        studentId, term, year);
                    var pdfBytes = await _pdfGenerator.GeneratePDFAsync(reportData);

                    var fileName = $"{studentId}_{reportData.StudentName.Replace(" ", "_")}_ReportCard.pdf";
                    var filePath = System.IO.Path.Combine(savePath, fileName);
                    await _printer.SaveToFileAsync(pdfBytes, filePath);

                    processed++;
                    progress?.Report(new BatchProgressReport { Current = processed, Total = total });
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other students
                    System.Diagnostics.Debug.WriteLine(
                        $"Error generating report card for {studentId}: {ex.Message}");
                }
            }
        }
    }

    public class ReportCardGenerationException : Exception
    {
        public ReportCardGenerationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
