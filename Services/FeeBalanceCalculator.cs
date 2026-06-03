using System;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public static class FeeBalanceCalculator
    {
        public static decimal CalculateNewBalance(decimal currentBalance, decimal amountPaid)
        {
            if (currentBalance < 0m)
                throw new ArgumentOutOfRangeException(nameof(currentBalance), "Current balance cannot be negative.");

            if (amountPaid <= 0m)
                throw new ArgumentOutOfRangeException(nameof(amountPaid), "Payment amount must be greater than zero.");

            decimal newBalance = currentBalance - amountPaid;
            return newBalance < 0m ? 0m : newBalance;
        }
    }
}
