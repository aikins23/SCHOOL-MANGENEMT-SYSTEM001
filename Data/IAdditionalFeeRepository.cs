using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IAdditionalFeeRepository
    {
        Task EnsureSchemaAsync();
        Task<int> CreateAsync(AdditionalFee fee);
        Task<AdditionalFee> GetByIdAsync(int additionalFeeId);
        Task<IReadOnlyList<AdditionalFee>> GetRecentAsync(int take = 50);
        Task<IReadOnlyList<AdditionalFeeAmount>> GetAmountsAsync(int additionalFeeId);
        Task<bool> HasDuplicateOpenFeeAsync(AdditionalFee fee);
        Task<AdditionalFeePreview> PreviewAsync(int additionalFeeId);
        Task SubmitAsync(int additionalFeeId, string submittedBy);
        Task ApproveAndPostAsync(int additionalFeeId, string approvedBy);
        Task RejectAsync(int additionalFeeId, string rejectedBy, string reason);
    }
}
