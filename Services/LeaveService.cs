using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class LeaveService
    {
        private readonly ILeaveRepository _repository;

        public LeaveService(ILeaveRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<(bool Success, string Message)> ApplyForLeaveAsync(Models.LeaveRequest request)
        {
            try
            {
                if (request.EndDate.Date < request.StartDate.Date)
                {
                    return (false, "End date cannot be before start date.");
                }

                request.Status = "PENDING";
                bool success = await _repository.AddLeaveRequestAsync(request);
                return success 
                    ? (true, "Leave application submitted successfully.") 
                    : (false, "Failed to submit leave application.");
            }
            catch (Exception ex)
            {
                return (false, "Error applying for leave: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateLeaveStatusAsync(Models.LeaveRequest request, string newStatus)
        {
            try
            {
                request.Status = newStatus;
                bool success = await _repository.UpdateLeaveRequestAsync(request);
                return success 
                    ? (true, $"Leave application {newStatus.ToLower()} successfully.") 
                    : (false, $"Failed to {newStatus.ToLower()} leave application.");
            }
            catch (Exception ex)
            {
                return (false, "Error updating leave status: " + ex.Message);
            }
        }

        public async Task<DataTable> GetLeaveRequestsTableAsync(string statusFilter = null)
        {
            if (string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "ALL")
            {
                return await _repository.GetAllLeaveRequestsTableAsync();
            }
            return await _repository.GetLeaveRequestsByStatusAsync(statusFilter);
        }

        public async Task<IEnumerable<Models.LeaveRequest>> GetStaffLeaveHistoryAsync(string employeeId)
        {
            return await _repository.GetEmployeeLeaveHistoryAsync(employeeId);
        }
    }
}
