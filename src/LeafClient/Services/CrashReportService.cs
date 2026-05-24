using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LeafClient.Models;
using LeafClient.PrivateServices;

namespace LeafClient.Services;

internal static class CrashReportService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(20) };
    private const string WorkerUrl = "https://backproxy.ziadfx.workers.dev/";

    internal sealed class SendResult
    {
        public bool NetworkSent { get; init; }
        public bool SavedLocally { get; init; }
        public string? SavedPath { get; init; }
        public string? SavedFolder { get; init; }
        public FailureKind Failure { get; init; } = FailureKind.None;
        public string? FailureMessage { get; init; }
        public string? PayloadJson { get; init; }
    }

    internal enum FailureKind
    {
        None,
        Timeout,
        IspBlockOrTlsIntercept,
        NetworkUnreachable,
        ServerRejected,
        Unknown,
    }

    public static async Task<SendResult> SendAsync(
        Exception exception,
        byte[]? screenshotBytes,
        LauncherSettings? settings)
    {
        string? payloadJson = null;
        string? savedPath = null;
        string? savedFolder = null;
        bool savedLocally = false;

        try
        {
            string userId;
            string userName;
            if (settings is { IsLoggedIn: true } && !string.IsNullOrWhiteSpace(settings.SessionUuid))
            {
                userId = settings.SessionUuid;
                userName = settings.SessionUsername ?? "Unknown";
            }
            else if (settings is not null && !string.IsNullOrWhiteSpace(settings.SuggestionUserId))
            {
                userId = settings.SuggestionUserId;
                userName = "Anonymous";
            }
            else
            {
                userId = $"anon_{Guid.NewGuid().ToString("N")[..8]}";
                userName = "Anonymous";
            }

            string exceptionType  = exception.GetType().FullName ?? exception.GetType().Name;
            string sanitizedStack = StackTraceSanitizer.Sanitize(exception.ToString());

            string logContent = $"[Auto-submitted crash report]\n" +
                                $"Exception:  {exceptionType}\n" +
                                $"Message:    {exception.Message}\n" +
                                $"OS:         {Environment.OSVersion}\n" +
                                $"Launcher:   {GetLauncherVersion()}\n\n" +
                                $"Stack trace:\n{sanitizedStack}";

            if (screenshotBytes is { Length: > 0 })
            {
                LeafLog.Info("CrashReport", $"Screenshot bytes={screenshotBytes.Length} dropped (bug-report schema has no image field).");
            }

            var payload = new BugReportPayload(
                Type:               "Bug Report",
                Expected:           "Launcher runs without unhandled exceptions.",
                Reality:            $"{exceptionType}: {exception.Message}",
                StepsToReproduce:   "Auto-submitted from the in-app crash reporter overlay.",
                LogFileContent:     logContent,
                OsVersion:          Environment.OSVersion.ToString(),
                LeafClientVersion:  GetLauncherVersion(),
                UserId:             userId,
                UserName:           userName,
                Timestamp:          DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            payloadJson = JsonSerializer.Serialize(payload, JsonContext.Default.BugReportPayload);
            LeafLog.Info("CrashReport", $"Payload size: {payloadJson.Length} chars (posted as Bug Report).");

            try
            {
                (savedPath, savedFolder) = SaveLocally(payloadJson, logContent, exceptionType, exception.Message);
                savedLocally = savedPath != null;
                if (savedLocally)
                    LeafLog.Info("CrashReport", $"Saved locally to {savedPath}");
            }
            catch (Exception saveEx)
            {
                LeafLog.Error("CrashReport", $"Local save failed ({saveEx.GetType().Name}): {saveEx.Message}");
            }

            try
            {
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(WorkerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var bodyPreview = "";
                    try { bodyPreview = await response.Content.ReadAsStringAsync(); } catch { }
                    if (LooksLikeBlockPage(bodyPreview))
                    {
                        LeafLog.Error("CrashReport", "Worker returned a block-page HTML - treating as ISP intercept.");
                        return new SendResult
                        {
                            NetworkSent    = false,
                            SavedLocally   = savedLocally,
                            SavedPath      = savedPath,
                            SavedFolder    = savedFolder,
                            Failure        = FailureKind.IspBlockOrTlsIntercept,
                            FailureMessage = "Your network returned a block page instead of a real response.",
                            PayloadJson    = payloadJson,
                        };
                    }

                    LeafLog.Info("CrashReport", "Report sent successfully.");
                    return new SendResult
                    {
                        NetworkSent  = true,
                        SavedLocally = savedLocally,
                        SavedPath    = savedPath,
                        SavedFolder  = savedFolder,
                        PayloadJson  = payloadJson,
                    };
                }

                string errBody = "";
                try { errBody = await response.Content.ReadAsStringAsync(); } catch { }
                LeafLog.Error("CrashReport", $"Worker returned {(int)response.StatusCode} {response.StatusCode}: {errBody}");
                return new SendResult
                {
                    NetworkSent    = false,
                    SavedLocally   = savedLocally,
                    SavedPath      = savedPath,
                    SavedFolder    = savedFolder,
                    Failure        = FailureKind.ServerRejected,
                    FailureMessage = $"Server returned {(int)response.StatusCode} {response.StatusCode}.",
                    PayloadJson    = payloadJson,
                };
            }
            catch (TaskCanceledException)
            {
                LeafLog.Error("CrashReport", "Network send timed out.");
                return new SendResult
                {
                    NetworkSent    = false,
                    SavedLocally   = savedLocally,
                    SavedPath      = savedPath,
                    SavedFolder    = savedFolder,
                    Failure        = FailureKind.Timeout,
                    FailureMessage = "The request timed out after 20 seconds.",
                    PayloadJson    = payloadJson,
                };
            }
            catch (HttpRequestException httpEx)
            {
                bool tlsOrCertFailure = ExceptionTreeContains(httpEx,
                    ex => ex is AuthenticationException
                       || ex.Message.Contains("SSL",         StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("TLS",         StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("trust",       StringComparison.OrdinalIgnoreCase));

                var kind = tlsOrCertFailure ? FailureKind.IspBlockOrTlsIntercept : FailureKind.NetworkUnreachable;
                LeafLog.Error("CrashReport", $"HTTP send failed ({kind}): {httpEx.Message}");
                if (httpEx.InnerException != null)
                    LeafLog.Error("CrashReport", $"Inner: {httpEx.InnerException.GetType().Name}: {httpEx.InnerException.Message}");

                return new SendResult
                {
                    NetworkSent    = false,
                    SavedLocally   = savedLocally,
                    SavedPath      = savedPath,
                    SavedFolder    = savedFolder,
                    Failure        = kind,
                    FailureMessage = httpEx.Message,
                    PayloadJson    = payloadJson,
                };
            }
        }
        catch (Exception ex)
        {
            LeafLog.Error("CrashReport", $"Unexpected error ({ex.GetType().Name}): {ex.Message}");
            return new SendResult
            {
                NetworkSent    = false,
                SavedLocally   = savedLocally,
                SavedPath      = savedPath,
                SavedFolder    = savedFolder,
                Failure        = FailureKind.Unknown,
                FailureMessage = ex.Message,
                PayloadJson    = payloadJson,
            };
        }
    }

    private static (string? path, string? folder) SaveLocally(string payloadJson, string logContent, string exceptionType, string exceptionMessage)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(appData)) return (null, null);

        string folder = Path.Combine(appData, "LeafClient", "CrashReports");
        Directory.CreateDirectory(folder);

        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string safeType = SafeFileFragment(exceptionType.Split('.').LastOrDefault() ?? "Exception");
        string baseName = $"crash-{stamp}-{safeType}";
        string jsonPath = Path.Combine(folder, baseName + ".json");
        string txtPath  = Path.Combine(folder, baseName + ".txt");

        File.WriteAllText(jsonPath, payloadJson, Encoding.UTF8);

        var txt = new StringBuilder();
        txt.AppendLine("Leaf Client - Crash Report");
        txt.AppendLine("==========================");
        txt.AppendLine($"Timestamp:  {DateTime.Now:u}");
        txt.AppendLine($"Exception:  {exceptionType}");
        txt.AppendLine($"Message:    {exceptionMessage}");
        txt.AppendLine();
        txt.AppendLine(logContent);
        txt.AppendLine();
        txt.AppendLine("--- Raw payload JSON (for developer upload) ---");
        txt.AppendLine(payloadJson);
        File.WriteAllText(txtPath, txt.ToString(), Encoding.UTF8);

        return (jsonPath, folder);
    }

    private static string SafeFileFragment(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        return sb.ToString();
    }

    private static bool LooksLikeBlockPage(string body)
    {
        if (string.IsNullOrEmpty(body)) return false;
        string lower = body.Trim().ToLowerInvariant();
        if (!lower.StartsWith("<") && !lower.Contains("<html")) return false;

        return lower.Contains("laliga")
            || lower.Contains("juzgado")
            || lower.Contains("bloqueo")
            || lower.Contains("blocked")
            || lower.Contains("notice-container")
            || lower.Contains("<html");
    }

    private static bool ExceptionTreeContains(Exception ex, Func<Exception, bool> predicate)
    {
        var cur = ex;
        while (cur != null)
        {
            if (predicate(cur)) return true;
            cur = cur.InnerException;
        }
        return false;
    }

    private static string GetLauncherVersion()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            return asm.GetName().Version?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
