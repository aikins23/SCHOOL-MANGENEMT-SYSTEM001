using System;
using System.Drawing;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// School identity + admission fee, persisted as a single row (Id = 1) in the
    /// SchoolInformation table. Source of truth for what was previously hardcoded.
    /// </summary>
    public class SchoolInformation
    {
        public string Name { get; set; } = "KINGDOM PREPARATORY SCHOOL";
        public string Address { get; set; } = "AKIM ODA- ABENASE";
        public string PoBox { get; set; } = "P. O. BOX 7 AKIM ODA";
        public string GpsAddress { get; set; } = "";          // Ghana Post GPS, e.g. AO-1234-5678
        public string Phone1 { get; set; } = "0548050141";
        public string Phone2 { get; set; } = "0246087609";
        public string Email { get; set; } = "noreply@kingdomprep.edu.gh";
        public byte[] Logo { get; set; }                        // null => fall back to Resources/school_logo.png
        public decimal AdmissionFee { get; set; } = 100m;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Report card brand colours (ARGB ints). Defaults = the legacy hardcoded colours.
        public int PrimaryColorArgb { get; set; } = Color.FromArgb(9, 35, 96).ToArgb();    // header / Navy
        public int AccentColorArgb { get; set; } = Color.FromArgb(210, 190, 36).ToArgb();  // accent / Gold
        public int SecondaryColorArgb { get; set; } = Color.FromArgb(189, 214, 238).ToArgb(); // row tint / Light Blue

        /// <summary>"0548050141 / 0246087609" with blanks dropped.</summary>
        public string Phones
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Phone1)) return Phone2 ?? "";
                if (string.IsNullOrWhiteSpace(Phone2)) return Phone1 ?? "";
                return Phone1 + " / " + Phone2;
            }
        }
    }
}
