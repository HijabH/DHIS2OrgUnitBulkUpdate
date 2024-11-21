namespace OrgUnitBulkUpdate.Models
{
    public class HealthFacility
    {
        public string ID { get; set; } = string.Empty; // Health Facility ID (UID)
        public string Name { get; set; } = string.Empty; // Full Name
        public string ShortName { get; set; } = string.Empty; // Short Name
        public string ParentID { get; set; } = string.Empty; // Parent Organisation Unit ID
    }
}
