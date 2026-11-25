using OrgUnitBulkUpdate.Services;

namespace OrgUnitBulkUpdate
{
    class Program
    {
        private const string Usage = """
            Usage: OrgUnitBulkUpdate <ExcelFilePath> [--dry-run]

            Connection settings come from environment variables:
              DHIS2_BASE_URL   e.g. https://play.im.dhis2.org/stable-2-41-x
              DHIS2_USERNAME
              DHIS2_PASSWORD

            --dry-run   read and validate the sheet, but do not send anything to DHIS2
            """;

        static async Task<int> Main(string[] args)
        {
            var excelFilePath = args.FirstOrDefault(a => !a.StartsWith("--"));
            var dryRun = args.Contains("--dry-run");
            if (excelFilePath is null)
            {
                Console.WriteLine(Usage);
                return 2;
            }

            ReadResult read;
            try
            {
                read = new ExcelDataReader().ReadHealthFacilities(excelFilePath);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not read the Excel file: {ex.Message}");
                return 1;
            }

            Console.WriteLine($"Valid rows: {read.Facilities.Count}, rejected rows: {read.RowErrors.Count}");
            foreach (var error in read.RowErrors)
            {
                Console.WriteLine($"  {error}");
            }

            if (dryRun)
            {
                Console.WriteLine("Dry run: nothing was sent to DHIS2.");
                return read.RowErrors.Count == 0 ? 0 : 1;
            }

            var baseUrl = Environment.GetEnvironmentVariable("DHIS2_BASE_URL");
            var username = Environment.GetEnvironmentVariable("DHIS2_USERNAME");
            var password = Environment.GetEnvironmentVariable("DHIS2_PASSWORD");
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.Error.WriteLine("Set DHIS2_BASE_URL, DHIS2_USERNAME and DHIS2_PASSWORD first. See .env.example.");
                return 2;
            }

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var client = new DHIS2ApiClient(http, baseUrl, username, password);

            int updated = 0, failed = 0;
            foreach (var facility in read.Facilities)
            {
                var result = await client.UpdateHealthFacilityAsync(facility);
                Console.WriteLine($"  Row {facility.RowNumber}: {facility.ID} {facility.Name} -> {result.Message}");
                if (result.Success) updated++; else failed++;
            }

            Console.WriteLine($"Updated: {updated}, failed: {failed}, rejected before sending: {read.RowErrors.Count}");
            return failed == 0 && read.RowErrors.Count == 0 ? 0 : 1;
        }
    }
}
