using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class ClassService
    {
        private readonly IClassRepository _repository;

        public ClassService(IClassRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task InitializeAsync()
        {
            await _repository.EnsureTableExistsAsync();
        }

        public async Task<DataTable> GetClassesTableAsync()
        {
            return await _repository.GetAllClassesTableAsync();
        }

        public async Task<(bool Success, string Message)> SaveClassAsync(Models.ClassConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.ClassName)) return (false, "Class name is required.");
                if (config.TuitionFee < 0) return (false, "Tuition fee cannot be negative.");

                bool success = await _repository.SaveClassAsync(config);
                return success 
                    ? (true, "Class configuration saved successfully.") 
                    : (false, "Failed to save class configuration.");
            }
            catch (Exception ex)
            {
                return (false, "Error saving class: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> DeleteClassAsync(string className)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(className)) return (false, "Class name is required.");
                bool success = await _repository.DeleteClassAsync(className);
                return success 
                    ? (true, "Class configuration deleted successfully.") 
                    : (false, "Failed to delete class configuration.");
            }
            catch (Exception ex)
            {
                return (false, "Error deleting class: " + ex.Message);
            }
        }

        public async Task<Models.ClassConfig> GetClassAsync(string className)
        {
            return await _repository.GetByClassNameAsync(className);
        }
    }
}
