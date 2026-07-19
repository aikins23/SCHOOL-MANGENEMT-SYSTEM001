using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class ScholarshipService
    {
        private readonly Data.ScholarshipRepository _repository;

        public ScholarshipService(Data.ScholarshipRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Calculates the adjusted tuition fee for a student after applying all their active scholarships.
        /// Fixed discounts are subtracted first, then percentage discounts are applied to the remaining balance.
        /// </summary>
        public async Task<decimal> CalculateAdjustedTuitionAsync(string studentId, decimal baseTuitionFee)
        {
            var scholarships = await _repository.GetStudentScholarshipsAsync(studentId);
            var approvedScholarships = scholarships.Where(s => s.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)).ToList();
            if (!approvedScholarships.Any()) return baseTuitionFee;

            decimal adjustedFee = baseTuitionFee;

            // 1. Apply FIXED discounts first
            var fixedDiscounts = approvedScholarships.Where(s => s.DiscountType == "FIXED");
            foreach (var f in fixedDiscounts)
            {
                adjustedFee -= f.DiscountValue;
            }

            // Ensure fee doesn't go below 0 after fixed discounts
            if (adjustedFee < 0) adjustedFee = 0;

            // 2. Apply PERCENTAGE discounts on the remaining balance
            var percentDiscounts = approvedScholarships.Where(s => s.DiscountType == "PERCENTAGE");
            foreach (var p in percentDiscounts)
            {
                decimal reduction = adjustedFee * (p.DiscountValue / 100m);
                adjustedFee -= reduction;
            }

            if (adjustedFee < 0) adjustedFee = 0;
            return Math.Round(adjustedFee, 2);
        }
    }
}
