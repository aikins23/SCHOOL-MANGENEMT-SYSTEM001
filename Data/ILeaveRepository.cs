using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ILeaveRepository
    {
        Task<bool> AddLeaveRequestAsync(KingdomPrep.Shared.Models.LeaveRequest request);
        Task<bool> UpdateLeaveRequestAsync(KingdomPrep.Shared.Models.LeaveRequest request);
        Task<bool> DeleteLeaveRequestAsync(int leaveId);
        Task<DataTable> GetAllLeaveRequestsTableAsync();
        Task<DataTable> GetLeaveRequestsByStatusAsync(string status);
        Task<IEnumerable<KingdomPrep.Shared.Models.LeaveRequest>> GetEmployeeLeaveHistoryAsync(string employeeId);
        Task<int> GetApprovedDaysInRangeAsync(string employeeId, System.DateTime termStart, System.DateTime termEnd);
        Task<bool> HasApprovedOverlapAsync(string employeeId, System.DateTime startDate, System.DateTime endDate);
        Task<DataTable> GetLeaveBalanceTableAsync(System.DateTime termStart, System.DateTime termEnd, int entitlement);
    }
}
