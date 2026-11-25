using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using OrgUnitBulkUpdate.Services;
using Xunit;

namespace OrgUnitBulkUpdate.Tests;

public class DHIS2ApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = "";

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            return _respond(request);
        }
    }

    private static DHIS2ApiClient Client(StubHandler handler) =>
        new(new HttpClient(handler), "https://dhis.example.org", "admin", "district");

    [Theory]
    [InlineData("https://dhis.example.org", "https://dhis.example.org/api/organisationUnits/")]
    [InlineData("https://dhis.example.org/", "https://dhis.example.org/api/organisationUnits/")]
    [InlineData("https://host/dhis/api", "https://host/dhis/api/organisationUnits/")]
    public void Builds_the_organisation_units_url(string input, string expected)
    {
        Assert.Equal(expected, DHIS2ApiClient.OrganisationUnitsUrl(input));
    }

    [Fact]
    public void Patch_changes_only_name_short_name_and_parent()
    {
        var patch = DHIS2ApiClient.BuildPatch(ValidatorTests.Valid());

        Assert.Equal(3, patch.Count);
        Assert.Equal("/name", patch[0]!["path"]!.GetValue<string>());
        Assert.Equal("/shortName", patch[1]!["path"]!.GetValue<string>());
        Assert.Equal("YuQRtpLP10I", patch[2]!["value"]!["id"]!.GetValue<string>());
        Assert.DoesNotContain("openingDate", patch.ToJsonString());
    }

    [Fact]
    public void Patch_leaves_parent_alone_when_none_is_given()
    {
        var facility = ValidatorTests.Valid();
        facility.ParentID = "";

        var patch = DHIS2ApiClient.BuildPatch(facility);

        Assert.Equal(2, patch.Count);
        Assert.DoesNotContain("/parent", patch.ToJsonString());
    }

    [Fact]
    public async Task Sends_a_json_patch_request_to_the_unit_with_basic_auth()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await Client(handler).UpdateHealthFacilityAsync(ValidatorTests.Valid());

        Assert.True(result.Success);
        var request = handler.LastRequest!;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://dhis.example.org/api/organisationUnits/DiszpKrYNg8", request.RequestUri!.ToString());
        Assert.Equal("application/json-patch+json", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(3, JsonNode.Parse(handler.LastBody)!.AsArray().Count);
    }

    [Fact]
    public async Task Returns_dhis2_error_reports_on_failure()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"status":"ERROR","response":{"errorReports":[{"message":"Parent not found"}]}}""",
                Encoding.UTF8, "application/json"),
        });

        var result = await Client(handler).UpdateHealthFacilityAsync(ValidatorTests.Valid());

        Assert.False(result.Success);
        Assert.Equal("HTTP 409: Parent not found", result.Message);
    }

    [Fact]
    public async Task Network_errors_are_reported_instead_of_crashing()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await Client(handler).UpdateHealthFacilityAsync(ValidatorTests.Valid());

        Assert.False(result.Success);
        Assert.Equal("Request failed: connection refused", result.Message);
    }

    [Fact]
    public void Error_message_falls_back_to_raw_body()
    {
        Assert.Equal("Bad gateway", DHIS2ApiClient.ReadErrorMessage("Bad gateway"));
    }
}
