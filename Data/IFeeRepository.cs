using System;
using System.Data;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IFeeRepository
    {
        Task<bool> AddInitialFeeRecordAsync(string studentId, string classId, decimal amount);
        Task<bool> AddInitialPaymentRecordAsync(string studentId, string classId, string studentName, decimal balance);
        Task<bool> UpdateFeeRecordAsync(string studentId, string classId, decimal amount);
        Task<bool> UpdatePaymentRecordAsync(string studentId, string classId, string studentName, decimal balance);
        Task<decimal?> GetLatestBalanceAsync(string studentId);
        Task<decimal?> GetDefaultBalanceAsync(string studentId, string classId);
        Task<bool> AddPaymentRecordAsync(string studentId, string classId, string studentName, decimal amountPaid, decimal newBalance, string paymentMode, string bursarName, DateTime date);
        Task<DataTable> GetPaymentHistoryTableAsync();
        Task<DataTable> GetOutstandingBalancesTableAsync();
        Task<DataTable> GetFeesTableAsync();
    }
}
