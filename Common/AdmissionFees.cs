namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// One-time admission fee. Reads from School Information settings (SchoolProfile),
    /// falling back to the model default (100) when settings are unavailable.
    /// </summary>
    public static class AdmissionFees
    {
        public static decimal Amount => SchoolProfile.AdmissionFee;
    }
}
