using System.Text.RegularExpressions;
using OrgUnitBulkUpdate.Models;

namespace OrgUnitBulkUpdate.Services
{
    public static partial class HealthFacilityValidator
    {
        // DHIS2 UIDs are 11 characters: a letter followed by 10 letters or digits
        [GeneratedRegex("^[A-Za-z][A-Za-z0-9]{10}$")]
        private static partial Regex UidPattern();

        // DHIS2 limits for organisation units
        public const int MaxNameLength = 230;
        public const int MaxShortNameLength = 50;

        public static bool IsValidUid(string value) => UidPattern().IsMatch(value);

        public static IReadOnlyList<string> Validate(HealthFacility facility)
        {
            var errors = new List<string>();

            if (!IsValidUid(facility.ID))
            {
                errors.Add($"ID '{facility.ID}' is not a valid DHIS2 UID (11 characters, starting with a letter)");
            }

            if (!string.IsNullOrEmpty(facility.ParentID))
            {
                if (!IsValidUid(facility.ParentID))
                {
                    errors.Add($"ParentID '{facility.ParentID}' is not a valid DHIS2 UID");
                }
                else if (facility.ParentID == facility.ID)
                {
                    errors.Add("ParentID cannot be the same as ID");
                }
            }

            if (string.IsNullOrWhiteSpace(facility.Name))
            {
                errors.Add("Name is required");
            }
            else if (facility.Name.Length > MaxNameLength)
            {
                errors.Add($"Name is longer than {MaxNameLength} characters");
            }

            if (string.IsNullOrWhiteSpace(facility.ShortName))
            {
                errors.Add("ShortName is required");
            }
            else if (facility.ShortName.Length > MaxShortNameLength)
            {
                errors.Add($"ShortName is longer than {MaxShortNameLength} characters");
            }

            return errors;
        }
    }
}
