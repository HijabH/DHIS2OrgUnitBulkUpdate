using System;
using System.Linq;
using System.Threading.Tasks;
using OrgUnitBulkUpdate.Services;

namespace OrgUnitBulkUpdate
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Program started...");

            // Check if the required arguments are passed
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: OrgUnitBulkUpdate <ExcelFilePath> <DHIS2BaseUrl>");
                return;
            }

            string excelFilePath = args[0];
            string dhis2BaseUrl = args[1];

            Console.WriteLine($"Excel File Path: {excelFilePath}");
            Console.WriteLine($"DHIS2 Base URL: {dhis2BaseUrl}");

            var excelReader = new ExcelDataReader();

            try
            {
                var healthFacilities = excelReader.ReadHealthFacilities(excelFilePath);
                Console.WriteLine($"Number of health facilities found: {healthFacilities.Count()}");

                var dhis2Client = new DHIS2ApiClient(dhis2BaseUrl);

                foreach (var facility in healthFacilities)
                {
                    Console.WriteLine($"Updating facility: ID = {facility.ID}, Name = {facility.Name}, ShortName = {facility.ShortName}, ParentID = {facility.ParentID}");
                    var success = await dhis2Client.UpdateHealthFacilityAsync(facility);
                    Console.WriteLine($"Update result: {(success ? "Success" : "Failed")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("Program completed.");
        }
    }
}
