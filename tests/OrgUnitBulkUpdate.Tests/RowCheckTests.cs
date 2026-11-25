using OrgUnitBulkUpdate.Models;
using OrgUnitBulkUpdate.Services;
using Xunit;

namespace OrgUnitBulkUpdate.Tests;

public class RowCheckTests
{
    [Fact]
    public void Keeps_valid_rows_and_reports_invalid_ones_with_row_numbers()
    {
        var bad = ValidatorTests.Valid("bad-id", 3);

        var result = ExcelDataReader.Check(new[] { ValidatorTests.Valid(), bad });

        Assert.Single(result.Facilities);
        var error = Assert.Single(result.RowErrors);
        Assert.StartsWith("Row 3:", error);
        Assert.Contains("'bad-id' is not a valid DHIS2 UID", error);
    }

    [Fact]
    public void Skips_duplicate_ids_after_the_first()
    {
        var result = ExcelDataReader.Check(new[]
        {
            ValidatorTests.Valid("DiszpKrYNg8", 2),
            ValidatorTests.Valid("DiszpKrYNg8", 5),
        });

        Assert.Equal(2, Assert.Single(result.Facilities).RowNumber);
        Assert.Equal("Row 5: duplicate ID 'DiszpKrYNg8', skipped", Assert.Single(result.RowErrors));
    }

    [Fact]
    public void Uids_are_case_sensitive_so_different_case_is_not_a_duplicate()
    {
        var result = ExcelDataReader.Check(new[]
        {
            ValidatorTests.Valid("DiszpKrYNg8", 2),
            ValidatorTests.Valid("diszpKrYNg8", 3),
        });

        Assert.Equal(2, result.Facilities.Count);
    }

    [Fact]
    public void Ignores_completely_blank_rows()
    {
        var result = ExcelDataReader.Check(new[] { new HealthFacility { RowNumber = 2 }, ValidatorTests.Valid(row: 3) });

        Assert.Single(result.Facilities);
        Assert.Empty(result.RowErrors);
    }
}
