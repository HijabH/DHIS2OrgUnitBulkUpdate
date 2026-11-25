using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrgUnitBulkUpdate.Models;

namespace OrgUnitBulkUpdate.Services
{
    public record UpdateResult(bool Success, string Message);

    public class DHIS2ApiClient
    {
        private readonly HttpClient _httpClient;

        public DHIS2ApiClient(HttpClient httpClient, string baseUrl, string username, string password)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(OrganisationUnitsUrl(baseUrl));

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Accepts the server address ("https://host/dhis") or the API address ("https://host/dhis/api")
        /// and returns the organisation units endpoint.
        /// </summary>
        public static string OrganisationUnitsUrl(string baseUrl)
        {
            var url = baseUrl.Trim().TrimEnd('/');
            if (!url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                url += "/api";
            }
            return url + "/organisationUnits/";
        }

        /// <summary>
        /// Builds a JSON Patch that changes only the fields from the sheet.
        /// A PUT would replace the whole organisation unit and reset fields such as the opening date and code.
        /// </summary>
        public static JsonArray BuildPatch(HealthFacility facility)
        {
            var patch = new JsonArray
            {
                new JsonObject { ["op"] = "replace", ["path"] = "/name", ["value"] = facility.Name },
                new JsonObject { ["op"] = "replace", ["path"] = "/shortName", ["value"] = facility.ShortName },
            };

            if (!string.IsNullOrEmpty(facility.ParentID))
            {
                patch.Add(new JsonObject
                {
                    ["op"] = "replace",
                    ["path"] = "/parent",
                    ["value"] = new JsonObject { ["id"] = facility.ParentID },
                });
            }

            return patch;
        }

        /// <summary>Updates one organisation unit with PATCH (DHIS2 2.37 and later).</summary>
        public async Task<UpdateResult> UpdateHealthFacilityAsync(HealthFacility facility, CancellationToken ct = default)
        {
            var content = new StringContent(BuildPatch(facility).ToJsonString(), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, facility.ID) { Content = content };
                using var response = await _httpClient.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                return response.IsSuccessStatusCode
                    ? new UpdateResult(true, "Updated")
                    : new UpdateResult(false, $"HTTP {(int)response.StatusCode}: {ReadErrorMessage(body)}");
            }
            catch (HttpRequestException ex)
            {
                return new UpdateResult(false, $"Request failed: {ex.Message}");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return new UpdateResult(false, "Request timed out");
            }
        }

        internal static string ReadErrorMessage(string body)
        {
            try
            {
                var node = JsonNode.Parse(body);
                var reports = node?["response"]?["errorReports"]?.AsArray();
                if (reports is { Count: > 0 })
                {
                    return string.Join("; ", reports
                        .Select(r => r?["message"]?.GetValue<string>())
                        .Where(m => !string.IsNullOrWhiteSpace(m)));
                }
                return node?["message"]?.GetValue<string>() ?? Shorten(body);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                return Shorten(body);
            }
        }

        private static string Shorten(string s) => s.Length <= 200 ? s : s[..200] + "...";
    }
}
