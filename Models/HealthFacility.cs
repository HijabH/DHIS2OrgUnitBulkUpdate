namespace OrgUnitBulkUpdate.Models
{
    /// <summary>One row of the Excel sheet: the new values for an existing organisation unit.</summary>
    public class HealthFacility
    {
        public int RowNumber { get; set; } // row in the Excel sheet, for error messages
        public string ID { get; set; } = string.Empty; // Health Facility ID (UID)
        public string Name { get; set; } = string.Empty; // Full Name
        public string ShortName { get; set; } = string.Empty; // Short Name
        public string ParentID { get; set; } = string.Empty; // Parent Organisation Unit ID (optional)
    }
}
