using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public interface IDbConnectionFactory
    {
        Task<SqlConnection> CreateOpenAsync(CancellationToken ct = default);
        string ConnectionString { get; }
    }
}
