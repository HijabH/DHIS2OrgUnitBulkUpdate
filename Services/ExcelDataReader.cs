using OfficeOpenXml;
using OrgUnitBulkUpdate.Models;

namespace OrgUnitBulkUpdate.Services
{
    public record ReadResult(IReadOnlyList<HealthFacility> Facilities, IReadOnlyList<string> RowErrors);

    public class ExcelDataReader
    {
        /// <summary>
        /// Reads the first worksheet. Columns: 1 ParentID, 2 ID, 3 Name, 4 ShortName. Row 1 is the header.
        /// </summary>
        public ReadResult ReadHealthFacilities(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            }

            // EPPlus 7 is free for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException("No worksheets found in the Excel file.");

            if (worksheet.Dimension is null)
            {
                return new ReadResult(Array.Empty<HealthFacility>(), Array.Empty<string>());
            }

            var rows = new List<HealthFacility>();
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // skip header row
            {
                rows.Add(new HealthFacility
                {
                    RowNumber = row,
                    ParentID = worksheet.Cells[row, 1].Text.Trim(),
                    ID = worksheet.Cells[row, 2].Text.Trim(),
                    Name = worksheet.Cells[row, 3].Text.Trim(),
                    ShortName = worksheet.Cells[row, 4].Text.Trim(),
                });
            }

            return Check(rows);
        }

        /// <summary>Validates rows and removes duplicates. Kept separate from Excel so it can be tested.</summary>
        internal static ReadResult Check(IEnumerable<HealthFacility> rows)
        {
            var facilities = new List<HealthFacility>();
            var errors = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal); // DHIS2 UIDs are case-sensitive

            foreach (var facility in rows)
            {
                if (string.IsNullOrEmpty(facility.ID) && string.IsNullOrEmpty(facility.Name)
                    && string.IsNullOrEmpty(facility.ShortName) && string.IsNullOrEmpty(facility.ParentID))
                {
                    continue; // blank row
                }

                var problems = HealthFacilityValidator.Validate(facility);
                if (problems.Count > 0)
                {
                    errors.Add($"Row {facility.RowNumber}: {string.Join("; ", problems)}");
                    continue;
                }

                if (!seenIds.Add(facility.ID))
                {
                    errors.Add($"Row {facility.RowNumber}: duplicate ID '{facility.ID}', skipped");
                    continue;
                }

                facilities.Add(facility);
            }

            return new ReadResult(facilities, errors);
        }
    }
}
