using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LeafClient.Models;
using LeafClient.Services;
using LeafClient;

namespace LeafClient.PrivateServices
{
    internal enum SuggestionType
    {
        Feature,
        Bug
    }

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

    internal record CrashReportPayload(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("exceptionType")] string ExceptionType,
        [property: JsonPropertyName("exceptionMessage")] string ExceptionMessage,
        [property: JsonPropertyName("sanitizedStackTrace")] string SanitizedStackTrace,
        [property: JsonPropertyName("screenshotBase64")] string? ScreenshotBase64,
        [property: JsonPropertyName("osVersion")] string OsVersion,
        [property: JsonPropertyName("leafClientVersion")] string LeafClientVersion,
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
                Type: "App Suggestion",
                Message: trimmedMessage,
                UserId: userId,
                UserName: userName,
                Timestamp: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            return await _sendToWorkerAsync(payload);
        }

        public async Task<(bool Success, string Message)> SendBugReportAsync(
            string expectedBehavior,
            string actualBehavior,
            string stepsToReproduce,
            string? logFileContent,
            string? osVersion,
            string? leafClientVersion)
        {
            if (string.IsNullOrWhiteSpace(expectedBehavior) ||
                string.IsNullOrWhiteSpace(actualBehavior) ||
                string.IsNullOrWhiteSpace(stepsToReproduce))
            {
                return (false, "Please fill in all required fields for the bug report (Expected, Reality, Steps to reproduce).");
            }

            var settings = await _settingsService.LoadSettingsAsync();
            var (userId, userName) = await GetUserIdentifiersAsync(settings);

            var payload = new BugReportPayload(
                Type: "Bug Report",
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

        private async Task<(string UserId, string UserName)> GetUserIdentifiersAsync(LauncherSettings settings)
        {
            string effectiveUserId;
            string effectiveUserName;

            if (settings.IsLoggedIn && !string.IsNullOrWhiteSpace(settings.SessionUuid))
            {
                effectiveUserId = settings.SessionUuid;
                effectiveUserName = settings.SessionUsername ?? "Unknown Minecraft User";
            }
            else
            {
                effectiveUserId = await GetOrCreateAnonymousSuggestionUserIdAsync(settings);
                effectiveUserName = "Anonymous";
            }
            return (effectiveUserId, effectiveUserName);
        }

        private async Task<string> GetOrCreateAnonymousSuggestionUserIdAsync(LauncherSettings settings)
        {
            if (string.IsNullOrEmpty(settings.SuggestionUserId))
            {
                settings.SuggestionUserId = $"anon_user_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                await _settingsService.SaveSettingsAsync(settings);
            }
            return settings.SuggestionUserId;
        }

        private static string GetFeedbackSaveDirectory() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LeafClient", "Feedback");

        private async Task SaveFeedbackLocallyAsync<T>(T payload, string typeSuffix) where T : class
        {
            try
            {
                var dir = GetFeedbackSaveDirectory();
                Directory.CreateDirectory(dir);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var path = Path.Combine(dir, $"feedback-{timestamp}-{typeSuffix}.json");
                var json = JsonSerializer.Serialize(payload, JsonContext.Default.GetTypeInfo(typeof(T))!);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                LeafLog.Error("SuggestionsService", $"Failed to save feedback locally: {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> _sendToWorkerAsync<T>(T payload) where T : class
        {
            var typeSuffix = payload is FeatureSuggestionPayload ? "Suggestion" : "BugReport";
            await SaveFeedbackLocallyAsync(payload, typeSuffix);

            try
            {
                var json = JsonSerializer.Serialize(payload, JsonContext.Default.GetTypeInfo(typeof(T))!);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(WorkerUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Thank you for your feedback! ✅");
                }
                else
                {
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonContext.Default.ErrorResponse);
                        return (false, errorResult?.Error ?? "Failed to send feedback. Please try again.");
                    }
                    catch (Exception exJson)
                    {
                        LeafLog.Error("SuggestionsService ERROR", $"Failed to deserialize error response: {exJson.Message}. Response: {responseContent}");
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
                LeafLog.Error("SuggestionsService ERROR", $"Network error: {exHttp.Message}");
                return (false, "Network error. Please check your internet connection.");
            }
            catch (Exception ex)
            {
                LeafLog.Error("SuggestionsService ERROR", $"An unexpected error occurred: {ex.Message}");
                return (false, "An unexpected error occurred. Please try again.");
            }
        }
    }

    public class ErrorResponse
    {
        public string? Error { get; set; }
    }
}