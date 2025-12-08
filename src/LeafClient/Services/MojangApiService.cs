using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class MojangApiService
    {
        private readonly HttpClient _httpClient;

        public MojangApiService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PlayerProfileResponse> ChangeName(string accessToken, string newUsername)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(newUsername))
                throw new ArgumentException("New username cannot be null or empty.", nameof(newUsername));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string url = $"https://api.minecraftservices.com/minecraft/profile/name/{Uri.EscapeDataString(newUsername)}";
            var content = new StringContent("", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                // AOT-FIX: Use source-generated context for deserialization
                return JsonSerializer.Deserialize(jsonResponse, JsonContext.Default.PlayerProfileResponse) ?? new PlayerProfileResponse { Name = newUsername };
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"[MojangApiService] ChangeName failed: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    try
                    {
                        // AOT-FIX: Use source-generated context
                        var errorResponse = JsonSerializer.Deserialize(errorContent, JsonContext.Default.MojangErrorResponse);
                        if (errorResponse?.ErrorMessage != null && errorResponse.ErrorMessage.Contains("name change is not allowed"))
                        {
                            throw new Exception("Username change is on cooldown (30 days).");
                        }
                        else if (errorResponse?.ErrorMessage != null && errorResponse.ErrorMessage.Contains("already taken"))
                        {
                            throw new Exception($"Username '{newUsername}' is already taken.");
                        }
                        else
                        {
                            throw new Exception(errorResponse?.ErrorMessage ?? $"Failed to change name: Forbidden. {response.ReasonPhrase}");
                        }
                    }
                    catch (JsonException)
                    {
                        throw new Exception($"Failed to change name: Forbidden. {response.ReasonPhrase}. Response: {errorContent}");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new Exception($"Username '{newUsername}' is already taken.");
                }
                else
                {
                    try
                    {
                        // AOT-FIX: Use source-generated context
                        var errorResponse = JsonSerializer.Deserialize(errorContent, JsonContext.Default.MojangErrorResponse);
                        throw new Exception(errorResponse?.ErrorMessage ?? $"Failed to change name: {response.ReasonPhrase}");
                    }
                    catch (JsonException)
                    {
                        throw new Exception($"Failed to change name: {response.ReasonPhrase}. Response: {errorContent}");
                    }
                }
            }
        }

        public async Task<NameChangeStatusResponse> GetNameChangeStatus(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string url = "https://api.minecraftservices.com/minecraft/profile/namechange";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                // AOT-FIX: Use source-generated context
                return JsonSerializer.Deserialize(jsonResponse, JsonContext.Default.NameChangeStatusResponse) ?? new NameChangeStatusResponse();
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"[MojangApiService] GetNameChangeStatus failed: {response.StatusCode} - {errorContent}");
                try
                {
                    // AOT-FIX: Use source-generated context
                    var errorResponse = JsonSerializer.Deserialize(errorContent, JsonContext.Default.MojangErrorResponse);
                    throw new Exception(errorResponse?.ErrorMessage ?? $"Failed to get name change status: {response.ReasonPhrase}");
                }
                catch (JsonException)
                {
                    throw new Exception($"Failed to get name change status: {response.ReasonPhrase}. Response: {errorContent}");
                }
            }
        }

        public class PlayerProfileResponse
        {
            public string? Name { get; set; }
            public string? Id { get; set; }
        }

        public class MojangErrorResponse
        {
            public string? Error { get; set; }
            public string? ErrorMessage { get; set; }
            public string? DeveloperMessage { get; set; }
        }

        public class NameChangeStatusResponse
        {
            public bool ProfileChangeAllowed { get; set; }
            public long? NextChangeDate { get; set; }
            public int NameChangeCooldownDays { get; set; }

            public string GetNextChangeDateTimeFormatted()
            {
                if (NextChangeDate.GetValueOrDefault(0L) > 0)
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(NextChangeDate.Value);
                    return dateTimeOffset.LocalDateTime.ToString("MMMM dd, yyyy 'at' hh:mm tt");
                }
                return "N/A (check Mojang's official site)";
            }
        }
    }
}
