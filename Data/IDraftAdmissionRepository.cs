using System.Collections.Generic;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IDraftAdmissionRepository
    {
        Task EnsureTableAsync();
        Task<int> AddAsync(DraftAdmission draft);
        Task<IEnumerable<DraftAdmission>> GetPendingAsync();
        Task<int> CountPendingAsync();
        Task<DraftAdmission> GetByIdAsync(int draftId);
        Task<bool> DeleteAsync(int draftId);
    }
}
