using System;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class AdditionalFeeService
    {
        private readonly IAdditionalFeeRepository _repository;

        public AdditionalFeeService(IAdditionalFeeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<(bool Success, string Message, int AdditionalFeeId)> CreateDraftAsync(AdditionalFee fee)
        {
            var validation = ValidateFee(fee);
            if (!validation.Success) return (false, validation.Message, 0);

            fee.Status = AdditionalFeeStatuses.Draft;
            if (await _repository.HasDuplicateOpenFeeAsync(fee))
                return (false, "An open additional fee with the same name, academic year, and term already exists.", 0);

            int id = await _repository.CreateAsync(fee);
            return (true, "Additional fee draft created.", id);
        }

        public async Task<AdditionalFeePreview> PreviewAsync(int additionalFeeId)
        {
            return await _repository.PreviewAsync(additionalFeeId);
        }

        public async Task<(bool Success, string Message)> SubmitForApprovalAsync(int additionalFeeId, string submittedBy)
        {
            var fee = await _repository.GetByIdAsync(additionalFeeId);
            if (fee == null) return (false, "Additional fee was not found.");
            if (!string.Equals(fee.Status, AdditionalFeeStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fee.Status, AdditionalFeeStatuses.ReturnedForCorrection, StringComparison.OrdinalIgnoreCase))
                return (false, "Only draft or returned fees can be submitted for approval.");

            var preview = await _repository.PreviewAsync(additionalFeeId);
            if (preview.StudentCount == 0)
                return (false, "No students match this additional fee. Review the selected classes or departments.");

            await _repository.SubmitAsync(additionalFeeId, submittedBy);
            return (true, "Additional fee submitted for approval.");
        }

        public async Task<(bool Success, string Message)> ApproveAsync(int additionalFeeId, string approvedBy)
        {
            var fee = await _repository.GetByIdAsync(additionalFeeId);
            if (fee == null) return (false, "Additional fee was not found.");
            if (!string.Equals(fee.Status, AdditionalFeeStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
                return (false, "Only fees pending approval can be approved.");
            if (!string.IsNullOrWhiteSpace(fee.CreatedBy) &&
                string.Equals(fee.CreatedBy.Trim(), (approvedBy ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return (false, "The person who created this fee cannot approve it. Another authorised user must review it.");

            await _repository.ApproveAndPostAsync(additionalFeeId, approvedBy);
            return (true, "Additional fee approved and posted to student accounts.");
        }

        public async Task<(bool Success, string Message)> RejectAsync(int additionalFeeId, string rejectedBy, string reason)
        {
            var fee = await _repository.GetByIdAsync(additionalFeeId);
            if (fee == null) return (false, "Additional fee was not found.");
            if (!string.Equals(fee.Status, AdditionalFeeStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
                return (false, "Only fees pending approval can be rejected.");

            await _repository.RejectAsync(additionalFeeId, rejectedBy, reason);
            return (true, "Additional fee rejected.");
        }

        private static (bool Success, string Message) ValidateFee(AdditionalFee fee)
        {
            if (fee == null) return (false, "Additional fee details are required.");
            if (string.IsNullOrWhiteSpace(fee.FeeName)) return (false, "Fee name is required.");
            if (string.IsNullOrWhiteSpace(fee.AcademicYear)) return (false, "Academic year is required.");
            if (string.IsNullOrWhiteSpace(fee.TermName)) return (false, "Term is required.");
            if (fee.DefaultAmount < 0) return (false, "Default amount cannot be negative.");
            if (fee.Amounts != null && fee.Amounts.Any(a => a.Amount < 0))
                return (false, "Additional fee amounts cannot be negative.");

            var mode = fee.AssignmentMode ?? AdditionalFeeAssignmentModes.Flat;
            if (!string.Equals(mode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mode, AdditionalFeeAssignmentModes.Class, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mode, AdditionalFeeAssignmentModes.Department, StringComparison.OrdinalIgnoreCase))
                return (false, "Select a valid fee assignment mode.");

            if (string.Equals(mode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase) &&
                fee.DefaultAmount <= 0)
                return (false, "Flat additional fees must have an amount greater than zero.");

            if (!string.Equals(mode, AdditionalFeeAssignmentModes.Flat, StringComparison.OrdinalIgnoreCase) &&
                (fee.Amounts == null || fee.Amounts.Count == 0))
                return (false, "Enter at least one class or department amount greater than zero. The flat amount is only used when Assignment Mode is Flat.");

            return (true, "");
        }
    }
}
