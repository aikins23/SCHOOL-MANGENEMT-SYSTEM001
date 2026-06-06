using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Drives the admission-approval workflow: validate the initial payment, save a
    /// draft (pending), and on approval promote it to a real Student + payment
    /// records. Nothing touches the live tables until ApproveAsync.
    /// </summary>
    public class DraftAdmissionService
    {
        private readonly IDraftAdmissionRepository _drafts;
        private readonly StudentService _students;
        private readonly IFeeRepository _fees;
        private readonly TransportRepository _transport = new TransportRepository(Common.AppConfig.ConnectionString);

        public DraftAdmissionService(IDraftAdmissionRepository drafts, StudentService students, IFeeRepository fees)
        {
            _drafts = drafts;
            _students = students;
            _fees = fees;
        }

        /// <summary>Minimum school fee payable at admission: half the term total.</summary>
        public static decimal MinSchoolFee(decimal termTotal) => Math.Round(termTotal / 2m, 2);

        public (bool Ok, string Message) Validate(DraftAdmission d)
        {
            if (d == null) return (false, "No admission data.");
            if (d.AdmissionFee < Common.AdmissionFees.Amount)
                return (false, $"Admission fee of GHS {Common.AdmissionFees.Amount:N2} is required.");
            if (d.SchoolFeePaid < MinSchoolFee(d.TermTotal))
                return (false, $"School fee must be at least 50% of GHS {d.TermTotal:N2} (GHS {MinSchoolFee(d.TermTotal):N2}).");
            return (true, "");
        }

        public async Task<(bool Ok, string Message)> CreateDraftAsync(DraftAdmission d)
        {
            var v = Validate(d);
            if (!v.Ok) return v;

            await _drafts.EnsureTableAsync();
            d.SubmittedDate = DateTime.Now;
            int id = await _drafts.AddAsync(d);
            return id > 0 ? (true, "Submitted for bursar approval.") : (false, "Could not save the draft admission.");
        }

        public async Task<IEnumerable<DraftAdmission>> GetPendingAsync()
        {
            await _drafts.EnsureTableAsync();
            return await _drafts.GetPendingAsync();
        }

        /// <summary>Pending-approval count for the dashboard notification bubble.
        /// Fail-safe: returns 0 if the table is missing or the query errors.</summary>
        public async Task<int> GetPendingCountAsync()
        {
            try
            {
                await _drafts.EnsureTableAsync();
                return await _drafts.CountPendingAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("GetPendingCountAsync failed: " + ex.Message);
                return 0;
            }
        }

        public Task<bool> RejectAsync(int draftId) => _drafts.DeleteAsync(draftId);

        /// <summary>
        /// Promotes a draft: creates the Student (+ initial school-fee balance), records
        /// the admission fee and the school-fee payment, deletes the draft, and returns
        /// the created student so the caller can print receipts + send the SMS.
        /// </summary>
        public async Task<(bool Ok, string Message, Student Student)> ApproveAsync(int draftId, string bursarName)
        {
            var d = await _drafts.GetByIdAsync(draftId);
            if (d == null) return (false, "Draft not found (already processed?).", null);

            var student = d.ToStudent();
            var add = await _students.AddStudentAsync(student); // creates Students + initial school-fee balance, assigns StudentID
            if (!add.Success) return (false, "Promotion failed: " + add.Message, null);

            decimal schoolBalanceAfter = Math.Max(0m, d.TermTotal - d.SchoolFeePaid);

            // Admission fee row first (carries the current school balance — does not change it).
            await _fees.AddPaymentRecordAsync(student.StudentID, student.ClassID, student.FullName,
                d.AdmissionFee, d.TermTotal, "Admission Fee", bursarName, DateTime.Today);

            // School-fee payment row LAST so GetLatestBalanceAsync returns the school balance.
            await _fees.AddPaymentRecordAsync(student.StudentID, student.ClassID, student.FullName,
                d.SchoolFeePaid, schoolBalanceAfter, string.IsNullOrWhiteSpace(d.PaymentMode) ? "Cash" : d.PaymentMode,
                bursarName, DateTime.Today);

            if (d.BusRouteId.HasValue && int.TryParse(student.StudentID, out int sidForBus))
            {
                try { await _transport.SetStudentRouteAsync(sidForBus, d.BusRouteId); }
                catch (Exception ex) { LoggerHelper.LogWarning("Transport link not saved for student " + student.StudentID + ": " + ex.Message); }
            }

            await _drafts.DeleteAsync(draftId);
            return (true, "Approved.", student);
        }
    }
}
