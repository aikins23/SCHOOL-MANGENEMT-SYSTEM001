using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ITransportRepository
    {
        Task EnsureTablesAsync();
        Task<List<Bus>> GetBusesAsync(string search);
        Task<int> AddBusAsync(Bus bus);
        Task UpdateBusAsync(Bus bus);
        Task<bool> DeleteBusAsync(int busId);
        Task<List<BusRoute>> GetRoutesAsync();
        Task<int> AddRouteAsync(BusRoute route);
        Task UpdateRouteAsync(BusRoute route);
        Task<bool> DeleteRouteAsync(int routeId);
        Task SetStudentRouteAsync(int studentId, int? routeId);
        Task<BusRoute> GetStudentRouteAsync(int studentId);
        Task<bool> AddTransportPaymentAsync(int studentId, int routeId, string periodKey, System.DateTime periodStart, System.DateTime periodEnd, decimal amountPaid, System.DateTime date, string cashier, string notes);
        Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey);
        Task<System.Data.DataTable> GetStudentTransportHistoryAsync(int studentId);
        Task<List<TransportArrear>> GetArrearsAsync(System.DateTime asOf, bool includeDaily);
        Task<List<TransportArrear>> GetReminderCandidatesAsync(System.DateTime asOf);
        Task LogReminderSentAsync(int studentId, string periodKey);
    }
}
