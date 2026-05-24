namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// School information for report card header (currently hardcoded, future: table-driven)
    /// </summary>
    public class SchoolInfo
    {
        public string Name { get; set; } = "KINGDOM PREPARATORY SCHOOL";
        public string Location { get; set; } = "AKIM ODA- ABENASE";
        public string PhoneNumbers { get; set; } = "0548050141/0246087609";
        public byte[] Logo { get; set; }               // School logo image bytes
    }
}
