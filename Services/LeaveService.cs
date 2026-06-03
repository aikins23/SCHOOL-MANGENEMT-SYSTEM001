using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class LeaveService
    {
        private readonly ILeaveRepository _repository;
        private readonly EmployeeService _employeeService;

        public LeaveService(ILeaveRepository repository)
            : this(repository, null) { }

        public LeaveService(ILeaveRepository repository, EmployeeService employeeService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _employeeService = employeeService;
        }

        public async Task<(bool Success, string Message)> ApplyForLeaveAsync(Models.LeaveRequest request)
        {
            try
            {
                if (request.EndDate.Date < request.StartDate.Date)
                {
                    return (false, "End date cannot be before start date.");
                }

                // Conflict detection: reject if the requested range overlaps an already-approved leave
                bool overlap = await _repository.HasApprovedOverlapAsync(
                    request.EmployeeID, request.StartDate.Date, request.EndDate.Date);
                if (overlap)
                {
                    return (false, "This date range overlaps with an existing approved leave for this employee.");
                }

                request.Status = "PENDING";
                bool success = await _repository.AddLeaveRequestAsync(request);
                if (success)
                {
                    _ = NotifyLeaveSubmittedAsync(request);
                }
                return success
                    ? (true, "Leave application submitted successfully.")
                    : (false, "Failed to submit leave application.");
            }
            catch (Exception ex)
            {
                return (false, "Error applying for leave: " + ex.Message);
            }
        }

        public async Task<LeaveBalance> GetLeaveBalanceAsync(string employeeId, string employeeName = null)
        {
            var term = AppConfig.Leave.CurrentTerm;
            int entitlement = AppConfig.Leave.DaysPerTerm;
            int used = string.IsNullOrWhiteSpace(employeeId)
                ? 0
                : await _repository.GetApprovedDaysInRangeAsync(employeeId, term.Start, term.End);

            return new LeaveBalance
            {
                EmployeeID = employeeId,
                EmployeeName = employeeName,
                TermName = term.TermName,
                TermStart = term.Start,
                TermEnd = term.End,
                Entitlement = entitlement,
                DaysUsed = used
            };
        }

        public async Task<DataTable> GetLeaveBalanceReportAsync()
        {
            var term = AppConfig.Leave.CurrentTerm;
            return await _repository.GetLeaveBalanceTableAsync(term.Start, term.End, AppConfig.Leave.DaysPerTerm);
        }

        public async Task<(bool Success, string Message)> UpdateLeaveStatusAsync(Models.LeaveRequest request, string newStatus)
        {
            try
            {
                request.Status = newStatus;
                bool success = await _repository.UpdateLeaveRequestAsync(request);
                if (success && (newStatus == "APPROVED" || newStatus == "REJECTED"))
                {
                    _ = NotifyLeaveDecisionAsync(request, newStatus);
                }
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

        // Fire-and-forget; resolves the employee's contact and sends email + SMS.
        private async Task NotifyLeaveSubmittedAsync(Models.LeaveRequest request)
        {
            if (_employeeService == null) return;
            try
            {
                var emp = await _employeeService.GetEmployeeAsync(request.EmployeeID);
                if (emp != null)
                {
                    if (!string.IsNullOrWhiteSpace(emp.Email))
                        _ = NotificationService.SendLeaveSubmittedAsync(
                            request.EmployeeName, emp.Email, request.StartDate, request.EndDate);
                    if (!string.IsNullOrWhiteSpace(emp.Contact))
                        _ = SmsService.SendLeaveSubmittedAsync(emp.Contact, request.EmployeeName);
                }

                string hrEmail = AppConfig.Notify.HrEmail;
                if (!string.IsNullOrWhiteSpace(hrEmail))
                    _ = NotificationService.SendLeaveRequestHrAlertAsync(
                        hrEmail, request.EmployeeName, request.StartDate, request.EndDate, request.Reason);

                string hrPhone = AppConfig.Notify.HrPhone;
                if (!string.IsNullOrWhiteSpace(hrPhone))
                    _ = SmsService.SendLeaveHrAlertAsync(
                        hrPhone, request.EmployeeName, request.StartDate, request.EndDate);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Leave-submitted notification failed", ex);
            }
        }

        private async Task NotifyLeaveDecisionAsync(Models.LeaveRequest request, string status)
        {
            if (_employeeService == null) return;
            try
            {
                var emp = await _employeeService.GetEmployeeAsync(request.EmployeeID);
                if (emp == null) return;
                if (!string.IsNullOrWhiteSpace(emp.Email))
                    _ = NotificationService.SendLeaveApprovalAsync(
                        request.EmployeeName, emp.Email, status, request.StartDate, request.EndDate, request.Reason);
                if (!string.IsNullOrWhiteSpace(emp.Contact))
                    _ = SmsService.SendLeaveDecisionAsync(
                        emp.Contact, request.EmployeeName, status, request.StartDate, request.EndDate);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Leave-decision notification failed", ex);
            }
        }
    }
}
