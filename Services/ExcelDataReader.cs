using OfficeOpenXml;
using OrgUnitBulkUpdate.Models;
using System.Collections.Generic;
using System.IO;

namespace OrgUnitBulkUpdate.Services
{
    public class ExcelDataReader
    {
        public IEnumerable<HealthFacility> ReadHealthFacilities(string filePath)
        {
            var healthFacilities = new List<HealthFacility>();
            var ids = new HashSet<string>(); // To track unique HealthFacilityIDs within the sheet

            // Set EPPlus license context to non-commercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assume first worksheet contains data
                if (worksheet == null)
                {
                    throw new Exception("No worksheets found in the Excel file.");
                }

                Console.WriteLine($"Processing sheet: {worksheet.Name}");

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Skip header row
                {
                    var parentId = worksheet.Cells[row, 1].Text; // Column 1: ParentID
                    var id = worksheet.Cells[row, 2].Text; // Column 2: Health Facility ID
                    var name = worksheet.Cells[row, 3].Text; // Column 3: Name
                    var shortName = worksheet.Cells[row, 4].Text; // Column 4: ShortName

                    if (string.IsNullOrEmpty(id))
                    {
                        Console.WriteLine($"Row {row} in sheet {worksheet.Name} has an empty HealthFacilityID. Skipping...");
                        continue; // Skip rows with no ID
                    }

                    if (ids.Contains(id))
                    {
                        // Duplicate detected
                        Console.WriteLine($"Duplicate HealthFacilityID '{id}' found in row {row}. Skipping...");
                        continue; // Skip the duplicate
                    }

                    ids.Add(id); // Add ID to the set
                    healthFacilities.Add(new HealthFacility
                    {
                        ID = id,
                        Name = name,
                        ShortName = shortName,
                        ParentID = parentId
                    });
                }
            }

            return healthFacilities;
        }
    }
}
