using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IGradingSchemeRepository
    {
        Task EnsureTableAsync();
        Task<List<GradeBand>> GetBandsAsync();
        Task SaveBandsAsync(IEnumerable<GradeBand> bands);
    }
}
