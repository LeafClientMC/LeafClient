using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JsonPropertyName
using System.Threading.Tasks;
using LeafClient.Models; // Required for LauncherSettings
using LeafClient.Services; // Required for SettingsService
using LeafClient; // Added for JsonContext

namespace LeafClient.PrivateServices
{
    // Enum to differentiate between feedback types
    internal enum SuggestionType
    {
        Feature,
        Bug
    }

    // Define concrete record types for the payloads for Source Generation
    // These need to be internal to be picked up by JsonContext if it's in the same assembly.
    // Added explicit JsonPropertyName attributes for clarity and robustness.
    internal record FeatureSuggestionPayload(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("userId")] string UserId,
        [property: JsonPropertyName("userName")] string UserName,
        [property: JsonPropertyName("timestamp")] string Timestamp
    );

    internal record BugReportPayload(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("expected")] string Expected,
        [property: JsonPropertyName("reality")] string Reality,
        [property: JsonPropertyName("stepsToReproduce")] string StepsToReproduce,
        [property: JsonPropertyName("logFileContent")] string? LogFileContent,
        [property: JsonPropertyName("osVersion")] string? OsVersion,
        [property: JsonPropertyName("leafClientVersion")] string? LeafClientVersion,
        [property: JsonPropertyName("userId")] string UserId,
        [property: JsonPropertyName("userName")] string UserName,
        [property: JsonPropertyName("timestamp")] string Timestamp
    );

    internal class SuggestionsService
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;
        private const string WorkerUrl = "https://backproxy.ziadfx.workers.dev/";

        public SuggestionsService(SettingsService settingsService)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _settingsService = settingsService;
        }

        /// <summary>
        /// Sends a feature suggestion to the feedback system.
        /// </summary>
        /// <param name="suggestionMessage">The user's feature suggestion.</param>
        /// <returns>A tuple indicating success and a message.</returns>
        public async Task<(bool Success, string Message)> SendFeatureSuggestionAsync(string suggestionMessage)
        {
            if (string.IsNullOrWhiteSpace(suggestionMessage))
                return (false, "Please enter a suggestion.");

            var trimmedMessage = suggestionMessage.Trim();
            if (trimmedMessage.Length == 0)
                return (false, "Suggestion cannot be empty.");
            if (trimmedMessage.Length > 1000)
                return (false, "Suggestion is too long (max 1000 characters).");

            var settings = await _settingsService.LoadSettingsAsync();
            var (userId, userName) = await GetUserIdentifiersAsync(settings);

            var payload = new FeatureSuggestionPayload(
                Type: "App Suggestion", // Changed to "App Suggestion"
                Message: trimmedMessage,
                UserId: userId,
                UserName: userName,
                Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            return await _sendToWorkerAsync(payload);
        }

        /// <summary>
        /// Sends a bug report to the feedback system.
        /// </summary>
        /// <param name="expectedBehavior">Description of the expected behavior.</param>
        /// <param name="actualBehavior">Description of what actually happened.</param>
        /// <param name="stepsToReproduce">Steps to reproduce the bug.</param>
        /// <param name="logFileContent">Optional content of the relevant log file.</param>
        /// <param name="osVersion">Optional operating system and version.</param>
        /// <param name="leafClientVersion">Optional Leaf Client version.</param>
        /// <returns>A tuple indicating success and a message.</returns>
        public async Task<(bool Success, string Message)> SendBugReportAsync(
            string expectedBehavior,
            string actualBehavior,
            string stepsToReproduce,
            string? logFileContent,
            string? osVersion,
            string? leafClientVersion)
        {
            // Basic validation for bug report required fields
            if (string.IsNullOrWhiteSpace(expectedBehavior) ||
                string.IsNullOrWhiteSpace(actualBehavior) ||
                string.IsNullOrWhiteSpace(stepsToReproduce))
            {
                return (false, "Please fill in all required fields for the bug report (Expected, Reality, Steps to reproduce).");
            }

            var settings = await _settingsService.LoadSettingsAsync();
            var (userId, userName) = await GetUserIdentifiersAsync(settings);

            var payload = new BugReportPayload(
                Type: "Bug Report", // Changed to "Bug Report"
                Expected: expectedBehavior.Trim(),
                Reality: actualBehavior.Trim(),
                StepsToReproduce: stepsToReproduce.Trim(),
                LogFileContent: logFileContent?.Trim(),
                OsVersion: osVersion?.Trim(),
                LeafClientVersion: leafClientVersion?.Trim(),
                UserId: userId,
                UserName: userName,
                Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            return await _sendToWorkerAsync(payload);
        }

        /// <summary>
        /// Retrieves the appropriate user ID (Minecraft UUID or anonymous) and username for feedback.
        /// </summary>
        private async Task<(string UserId, string UserName)> GetUserIdentifiersAsync(LauncherSettings settings)
        {
            string effectiveUserId;
            string effectiveUserName;

            if (settings.IsLoggedIn && !string.IsNullOrWhiteSpace(settings.SessionUuid))
            {
                effectiveUserId = settings.SessionUuid;
                effectiveUserName = settings.SessionUsername ?? "Unknown Minecraft User"; // Fallback if UUID exists but username is null
            }
            else
            {
                effectiveUserId = await GetOrCreateAnonymousSuggestionUserIdAsync(settings);
                effectiveUserName = "Anonymous";
            }
            return (effectiveUserId, effectiveUserName);
        }

        /// <summary>
        /// Retrieves a persistent anonymous user ID from settings, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="settings">The current LauncherSettings instance.</param>
        private async Task<string> GetOrCreateAnonymousSuggestionUserIdAsync(LauncherSettings settings)
        {
            if (string.IsNullOrEmpty(settings.SuggestionUserId))
            {
                settings.SuggestionUserId = $"anon_user_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                await _settingsService.SaveSettingsAsync(settings); // Save the new anonymous ID
            }
            return settings.SuggestionUserId;
        }

        /// <summary>
        /// Handles the actual HTTP POST request to the Cloudflare Worker.
        /// </summary>
        /// <param name="payload">The concrete payload object to serialize.</param>
        /// <returns>A tuple indicating success and a message.</returns>
        private async Task<(bool Success, string Message)> _sendToWorkerAsync<T>(T payload) where T : class
        {
            try
            {
                // Explicitly use the JsonContext.Default.GetTypeInfo for source generation
                // This is crucial when using source generation with specific types.
                var json = JsonSerializer.Serialize(payload, JsonContext.Default.GetTypeInfo(typeof(T)));
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(WorkerUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Thank you for your feedback! ✅"); // Generic success message for both
                }
                else
                {
                    // Try to parse error message from worker
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonContext.Default.ErrorResponse);
                        return (false, errorResult?.Error ?? "Failed to send feedback. Please try again.");
                    }
                    catch (Exception exJson)
                    {
                        Console.Error.WriteLine($"[SuggestionsService ERROR] Failed to deserialize error response: {exJson.Message}. Response: {responseContent}");
                        return (false, "Failed to send feedback. Server returned an unreadable error. Please try again.");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return (false, "Request timed out. Please check your connection and try again.");
            }
            catch (HttpRequestException exHttp)
            {
                Console.Error.WriteLine($"[SuggestionsService ERROR] Network error: {exHttp.Message}");
                return (false, "Network error. Please check your internet connection.");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes, but return a generic message to the user.
                Console.Error.WriteLine($"[SuggestionsService ERROR] An unexpected error occurred: {ex.Message}");
                return (false, "An unexpected error occurred. Please try again.");
            }
        }
    }

    public class ErrorResponse
    {
        public string? Error { get; set; } // Made nullable for safety
    }
}