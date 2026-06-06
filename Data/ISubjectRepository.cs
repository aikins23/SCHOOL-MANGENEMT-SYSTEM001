using System.Collections.Generic;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ISubjectRepository
    {
        Task EnsureTableAsync();
        Task<List<string>> GetSubjectsForClassAsync(string className);
        Task<Dictionary<string, List<string>>> GetAllAsync();
        Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects);
    }
}
