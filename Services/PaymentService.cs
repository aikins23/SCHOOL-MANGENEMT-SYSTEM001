using System;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public sealed class PaymentRecordRequest
    {
        public string StudentId { get; set; }
        public string ClassId { get; set; }
        public string StudentName { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMode { get; set; }
        public string BursarName { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public sealed class PaymentRecordResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal NewBalance { get; set; }
    }

    public class PaymentService
    {
        private readonly IFeeRepository _repository;

        public PaymentService(IFeeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<PaymentRecordResult> RecordPaymentAsync(PaymentRecordRequest request)
        {
            if (request == null)
                return Failure("Payment request is required.");

            string studentId = (request.StudentId ?? "").Trim();
            string classId = (request.ClassId ?? "").Trim();
            string studentName = (request.StudentName ?? "").Trim();
            string paymentMode = (request.PaymentMode ?? "").Trim();
            string bursarName = (request.BursarName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(studentId))
                return Failure("Student ID is required.");

            if (string.IsNullOrWhiteSpace(studentName))
                return Failure("Student name is required.");

            if (string.IsNullOrWhiteSpace(classId))
                return Failure("Class is required.");

            if (string.IsNullOrWhiteSpace(bursarName))
                return Failure("Bursar name is required.");

            if (request.PaymentDate == DateTime.MinValue)
                return Failure("Payment date is required.");

            try
            {
                decimal newBalance = FeeBalanceCalculator.CalculateNewBalance(
                    request.CurrentBalance,
                    request.AmountPaid);

                bool saved = await _repository.AddPaymentRecordAsync(
                    studentId,
                    classId,
                    studentName,
                    request.AmountPaid,
                    newBalance,
                    paymentMode,
                    bursarName,
                    request.PaymentDate.Date);

                return saved
                    ? new PaymentRecordResult
                    {
                        Success = true,
                        Message = "Payment recorded successfully.",
                        NewBalance = newBalance
                    }
                    : Failure("Could not save payment record.", newBalance);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Failure(ex.Message);
            }
            catch (DataException ex)
            {
                LoggerHelper.LogError("Payment record persistence failed", ex);
                return Failure("Could not save payment record: " + ex.Message);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Payment record failed", ex);
                return Failure("Record payment failed: " + ex.Message);
            }
        }

        private static PaymentRecordResult Failure(string message, decimal newBalance = 0m)
        {
            return new PaymentRecordResult
            {
                Success = false,
                Message = message,
                NewBalance = newBalance
            };
        }
    }
}
