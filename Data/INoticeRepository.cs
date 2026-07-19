using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface INoticeRepository
    {
        Task<IEnumerable<Notice>> GetAllAsync();
        Task<bool> AddAsync(Notice notice);
        Task<bool> DeleteAsync(int noticeId);
        Task<DataTable> GetAsTableAsync();
        Task<DataTable> GetRecipientTableAsync(string target, string targetClass);
        Task<bool> UpdateDeliveryStatusAsync(int noticeId, int recipientCount, string status);
    }
}
