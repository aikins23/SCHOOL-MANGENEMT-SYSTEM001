using System.Collections.Generic;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ISchoolInfoRepository
    {
        Task EnsureTablesAsync();
        Task<SchoolInformation> GetAsync();
        Task<Dictionary<string, decimal>> GetClassFeesAsync();
        Task<List<string>> GetClassNamesAsync();
        Task SaveAsync(SchoolInformation info);
        Task SaveClassFeesAsync(IDictionary<string, decimal> fees);
    }
}
