using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ILeaveRepository
    {
        Task<bool> AddLeaveRequestAsync(Models.LeaveRequest request);
        Task<bool> UpdateLeaveRequestAsync(Models.LeaveRequest request);
        Task<bool> DeleteLeaveRequestAsync(int leaveId);
        Task<DataTable> GetAllLeaveRequestsTableAsync();
        Task<DataTable> GetLeaveRequestsByStatusAsync(string status);
        Task<IEnumerable<Models.LeaveRequest>> GetEmployeeLeaveHistoryAsync(string employeeId);
    }
}
