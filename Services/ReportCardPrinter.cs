using KingdomPrep.Shared.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

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
        public async Task<string> PrintToPrinterAsync(byte[] pdfBytes, string printerName = null)
        {
            try
            {
                // If no printer specified, use default
                if (string.IsNullOrEmpty(printerName))
                    printerName = GetDefaultPrinterName();

                // Save temporarily
                var tempPath = Path.Combine(Path.GetTempPath(), $"ReportCard_{Guid.NewGuid()}.pdf");
                await Task.Run(() => File.WriteAllBytes(tempPath, pdfBytes));

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
                    return null;
                }
                catch (Exception ex) when (IsPdfShellPrintFailure(ex))
                {
                    var fallbackPath = GetFallbackReportPath();
                    File.Copy(tempPath, fallbackPath, overwrite: true);
                    LoggerHelper.LogWarning($"Windows could not print the PDF automatically; saved report card to '{fallbackPath}'. {ex.Message}");
                    return fallbackPath;
                }
                finally
                {
                    // Clean up temp file (ignore if locked)
                    try { File.Delete(tempPath); }
                    catch (Exception ex) { LoggerHelper.LogWarning($"Failed to delete temporary PDF file: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error printing report card", ex);
                throw new PrintingException("Error printing report card", ex);
            }
        }

        /// <summary>
        /// Saves PDF to the file system. If the target file is open in another
        /// program (e.g. a PDF viewer left open from a previous save), it falls back
        /// to a uniquely-numbered filename instead of failing. Returns the path
        /// actually written.
        /// </summary>
        public async Task<string> SaveToFileAsync(byte[] pdfBytes, string filePath)
        {
            try
            {
                // Create directory if needed
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return await Task.Run(() =>
                {
                    try
                    {
                        File.WriteAllBytes(filePath, pdfBytes);
                        return filePath;
                    }
                    catch (IOException) // target locked / in use by another process
                    {
                        var alt = GetUniquePath(filePath);
                        File.WriteAllBytes(alt, pdfBytes);
                        LoggerHelper.LogWarning(
                            $"'{filePath}' was in use (likely open in a viewer); saved to '{alt}' instead.");
                        return alt;
                    }
                });
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error saving report card to {filePath}", ex);
                throw new PrintingException($"Error saving report card to {filePath}", ex);
            }
        }

        /// <summary>Finds "name (1).pdf", "name (2).pdf", … that doesn't already exist.</summary>
        private static string GetUniquePath(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath) ?? "";
            var name = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);
            for (int i = 1; i < 1000; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate)) return candidate;
            }
            return Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
        }

        private static string GetFallbackReportPath()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var folder = Path.Combine(string.IsNullOrWhiteSpace(documents) ? Path.GetTempPath() : documents, "Nyansapo Report Cards");
            Directory.CreateDirectory(folder);
            return GetUniquePath(Path.Combine(folder, $"ReportCard_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"));
        }

        private static bool IsPdfShellPrintFailure(Exception ex)
        {
            if (ex is Win32Exception win32 && (win32.NativeErrorCode == 1155 || win32.NativeErrorCode == 31))
                return true;
            return ex != null && ex.Message.IndexOf("No application is associated", StringComparison.OrdinalIgnoreCase) >= 0;
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
                LoggerHelper.LogError("Error showing print dialog", ex);
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
            catch (Exception ex)
            {
                LoggerHelper.LogWarning($"Failed to get default printer name, will use system default: {ex.Message}");
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
