using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OrgUnitBulkUpdate.Models;

namespace OrgUnitBulkUpdate.Services
{
    public class DHIS2ApiClient
    {
        private readonly HttpClient _httpClient;

        public DHIS2ApiClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            // Hardcoded username and password
            string username = "admin";
            string password = "district";
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<bool> UpdateHealthFacilityAsync(HealthFacility facility)
        {
            try
            {
                // Include ParentId in the payload
                var jsonPayload = JsonSerializer.Serialize(new
                {
                    name = facility.Name,
                    shortName = facility.ShortName,
                    openingDate = "1970-01-01", // Replace with dynamic date if needed
                    parent = new
                    {
                        id = facility.ParentID // Use ParentId from Excel
                    }
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                Console.WriteLine($"Request Payload: {jsonPayload}");

                var response = await _httpClient.PutAsync($"{facility.ID}", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false;
            }
        }


    }
}
