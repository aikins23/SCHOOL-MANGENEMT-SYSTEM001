using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class NoticeService
    {
        private readonly INoticeRepository _repository;

        public NoticeService(INoticeRepository repository)
        {
            _repository = repository;
        }

        public async Task<DataTable> GetHistoryTableAsync()
        {
            return await _repository.GetAsTableAsync();
        }

        public async Task<(bool Success, string Message)> SendNoticeAsync(Notice notice)
        {
            // Integration with NotificationService and SmsService would go here
            bool success = await _repository.AddAsync(notice);
            return (success, success ? "Notice sent successfully" : "Failed to save notice");
        }
    }
}
