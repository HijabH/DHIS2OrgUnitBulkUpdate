using OrgUnitBulkUpdate.Models;
using OrgUnitBulkUpdate.Services;
using Xunit;

namespace OrgUnitBulkUpdate.Tests;

public class ValidatorTests
{
    internal static HealthFacility Valid(string id = "DiszpKrYNg8", int row = 2) => new()
    {
        RowNumber = row,
        ID = id,
        ParentID = "YuQRtpLP10I",
        Name = "Ngelehun CHC",
        ShortName = "Ngelehun CHC",
    };

    [Fact]
    public void Valid_row_has_no_errors()
    {
        Assert.Empty(HealthFacilityValidator.Validate(Valid()));
    }

    [Theory]
    [InlineData("DiszpKrYNg8", true)]
    [InlineData("a1234567890", true)]
    [InlineData("1iszpKrYNg8", false)] // must start with a letter
    [InlineData("DiszpKrYNg", false)]  // 10 characters
    [InlineData("DiszpKrYNg89", false)] // 12 characters
    [InlineData("Diszp-rYNg8", false)]
    [InlineData("", false)]
    public void Checks_uid_format(string uid, bool expected)
    {
        Assert.Equal(expected, HealthFacilityValidator.IsValidUid(uid));
    }

    [Fact]
    public void Parent_is_optional()
    {
        var facility = Valid();
        facility.ParentID = "";

        Assert.Empty(HealthFacilityValidator.Validate(facility));
    }

    [Fact]
    public void Parent_must_be_a_valid_uid_and_not_the_unit_itself()
    {
        var bad = Valid();
        bad.ParentID = "nope";
        Assert.Contains(HealthFacilityValidator.Validate(bad), e => e.StartsWith("ParentID 'nope'"));

        var self = Valid();
        self.ParentID = self.ID;
        Assert.Contains("ParentID cannot be the same as ID", HealthFacilityValidator.Validate(self));
    }

    [Fact]
    public void Name_and_short_name_are_required_and_limited()
    {
        var empty = Valid();
        empty.Name = " ";
        empty.ShortName = "";
        var errors = HealthFacilityValidator.Validate(empty);
        Assert.Contains("Name is required", errors);
        Assert.Contains("ShortName is required", errors);

        var tooLong = Valid();
        tooLong.Name = new string('a', 231);
        tooLong.ShortName = new string('b', 51);
        errors = HealthFacilityValidator.Validate(tooLong);
        Assert.Contains("Name is longer than 230 characters", errors);
        Assert.Contains("ShortName is longer than 50 characters", errors);
    }
}
