using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for handling report card output (printing to physical printer or saving to file)
    /// </summary>
    public class ReportCardPrinter
    {
        /// <summary>
        /// Sends PDF to physical printer (default or specified)
        /// </summary>
        public async Task PrintToPrinterAsync(byte[] pdfBytes, string printerName = null)
        {
            try
            {
                // If no printer specified, use default
                if (string.IsNullOrEmpty(printerName))
                    printerName = GetDefaultPrinterName();

                // Save temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), $"ReportCard_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempPath, pdfBytes);

                try
                {
                    // Print via Windows default PDF handler
                    var psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Verb = "print",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);

                    // Wait a bit for print to queue, then delete temp file
                    await Task.Delay(1000);
                }
                finally
                {
                    // Clean up temp file (ignore if locked)
                    try { File.Delete(tempPath); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                throw new PrintingException("Error printing report card", ex);
            }
        }

        /// <summary>
        /// Saves PDF to file system
        /// </summary>
        public async Task SaveToFileAsync(byte[] pdfBytes, string filePath)
        {
            try
            {
                // Create directory if needed
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllBytesAsync(filePath, pdfBytes);
            }
            catch (Exception ex)
            {
                throw new PrintingException($"Error saving report card to {filePath}", ex);
            }
        }

        /// <summary>
        /// Shows print dialog to user for printer selection
        /// </summary>
        public bool ShowPrintDialog(out string selectedPrinter)
        {
            selectedPrinter = null;

            try
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPrinter = dialog.PrinterSettings.PrinterName;
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new PrintingException("Error showing print dialog", ex);
            }

            return false;
        }

        private string GetDefaultPrinterName()
        {
            try
            {
                var settings = new System.Drawing.Printing.PrinterSettings();
                return settings.PrinterName;
            }
            catch
            {
                return null;  // Will use system default
            }
        }
    }

    public class PrintingException : Exception
    {
        public PrintingException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}
