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

    /// <summary>
    /// Outcome of a crash report attempt. A local save is attempted on every
    /// call and reported via <see cref="SavedPath"/> so the user can recover
    /// the report even when the network send is blocked by an ISP, a VPN,
    /// a firewall, or anything else.
    /// </summary>
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

    /// <summary>
    /// Builds a crash report payload, saves it locally for safekeeping, and
    /// attempts to submit it to the Cloudflare Worker proxy. Never throws.
    /// </summary>
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

            // Note: the Cloudflare Worker proxy whitelists the feedback "type" field
            // and currently only accepts "App Suggestion" and "Bug Report". A custom
            // "Crash Report" type is rejected with 400 "Invalid feedback type", so we
            // ship crashes as a specially-formatted Bug Report. The worker will route
            // it through the same pipeline the manual bug form already uses.
            string exceptionType  = exception.GetType().FullName ?? exception.GetType().Name;
            string sanitizedStack = StackTraceSanitizer.Sanitize(exception.ToString());

            string logContent = $"[Auto-submitted crash report]\n" +
                                $"Exception:  {exceptionType}\n" +
                                $"Message:    {exception.Message}\n" +
                                $"OS:         {Environment.OSVersion}\n" +
                                $"Launcher:   {GetLauncherVersion()}\n\n" +
                                $"Stack trace:\n{sanitizedStack}";

            // Keep screenshots out of the payload for now — the bug-report schema
            // doesn't carry an image field, and base64 screenshots would 3-5× the
            // body size and risk hitting the worker's request limit. Note and drop.
            if (screenshotBytes is { Length: > 0 })
            {
                Console.WriteLine($"[CrashReport] Screenshot bytes={screenshotBytes.Length} dropped (bug-report schema has no image field).");
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
            Console.WriteLine($"[CrashReport] Payload size: {payloadJson.Length} chars (posted as Bug Report).");

            // Step 1 — ALWAYS try to persist the report to disk first. This is the
            // user's lifeline when the network send is blocked (ISP intercept, VPN,
            // firewall, offline) because it means the report is never lost — they
            // can always ship the saved .json via Discord / email later.
            try
            {
                (savedPath, savedFolder) = SaveLocally(payloadJson, logContent, exceptionType, exception.Message);
                savedLocally = savedPath != null;
                if (savedLocally)
                    Console.WriteLine($"[CrashReport] Saved locally to {savedPath}");
            }
            catch (Exception saveEx)
            {
                Console.Error.WriteLine($"[CrashReport] Local save failed ({saveEx.GetType().Name}): {saveEx.Message}");
            }

            // Step 2 — Attempt the network send. Any failure here falls back to
            // the locally saved copy and is classified for the UI.
            try
            {
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(WorkerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // Some ISPs (notably the LaLiga court-ordered Cloudflare block
                    // in Spain) terminate TLS and return a HTTP 200 with an HTML
                    // block page. Treat that as a failure instead of a success so
                    // the user sees the real state.
                    var bodyPreview = "";
                    try { bodyPreview = await response.Content.ReadAsStringAsync(); } catch { }
                    if (LooksLikeBlockPage(bodyPreview))
                    {
                        Console.Error.WriteLine("[CrashReport] Worker returned a block-page HTML — treating as ISP intercept.");
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

                    Console.WriteLine("[CrashReport] Report sent successfully.");
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
                Console.Error.WriteLine($"[CrashReport] Worker returned {(int)response.StatusCode} {response.StatusCode}: {errBody}");
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
                Console.Error.WriteLine("[CrashReport] Network send timed out.");
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
                // The LaLiga / Telefonica block works by MITM-ing Cloudflare Worker
                // IPs with an untrusted TLS cert, which trips HttpClient's chain
                // validation. The resulting exception chain always bottoms out at
                // an AuthenticationException or a WinHttpException with a cert
                // status message. Surface that to the user so they know it's
                // their ISP, not a bug on our end.
                bool tlsOrCertFailure = ExceptionTreeContains(httpEx,
                    ex => ex is AuthenticationException
                       || ex.Message.Contains("SSL",         StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("TLS",         StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
                       || ex.Message.Contains("trust",       StringComparison.OrdinalIgnoreCase));

                var kind = tlsOrCertFailure ? FailureKind.IspBlockOrTlsIntercept : FailureKind.NetworkUnreachable;
                Console.Error.WriteLine($"[CrashReport] HTTP send failed ({kind}): {httpEx.Message}");
                if (httpEx.InnerException != null)
                    Console.Error.WriteLine($"[CrashReport]   Inner: {httpEx.InnerException.GetType().Name}: {httpEx.InnerException.Message}");

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
            Console.Error.WriteLine($"[CrashReport] Unexpected error ({ex.GetType().Name}): {ex.Message}");
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

    /// <summary>
    /// Writes the payload JSON and a human-readable text dump next to each
    /// other under %APPDATA%\LeafClient\CrashReports. Returns the .json path
    /// and the containing folder so the UI can open either one.
    /// </summary>
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
        txt.AppendLine("Leaf Client — Crash Report");
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

        // Heuristics for the Spanish LaLiga block page and other common
        // ISP/firewall intercept pages. JSON replies from the real worker
        // never start with HTML so any HTML body is a strong signal.
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
