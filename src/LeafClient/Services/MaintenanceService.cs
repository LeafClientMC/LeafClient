using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LeafClient.Services
{
    public class MaintenanceStatus
    {
        public bool ApiReachable { get; set; }
        public string IncidentTitle { get; set; } = "";
        public string IncidentBody { get; set; } = "";
        public string IncidentDate { get; set; } = "";
        public bool HasIncident => !string.IsNullOrWhiteSpace(IncidentTitle);
    }

    internal static class MaintenanceService
    {
        private const string ApiBase = "https://api.leafclient.com";
        private const string FeedUrl = "https://leafclient.com/status/feed.xml";
        private const string DiscordInvite = "https://discord.gg/wvYqKpUNdb";
        private const string StatusPageUrl = "https://leafclient.com/status";

        public static string DiscordUrl => DiscordInvite;
        public static string StatusUrl  => StatusPageUrl;

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8),
        };

        public static async Task<bool> IsApiReachableAsync(CancellationToken ct = default)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/news");
                req.Headers.UserAgent.ParseAdd("LeafClient/1.0 (maintenance-probe)");
                using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                return (int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<MaintenanceStatus> FetchStatusAsync(CancellationToken ct = default)
        {
            var status = new MaintenanceStatus { ApiReachable = false };
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, FeedUrl);
                req.Headers.UserAgent.ParseAdd("LeafClient/1.0 (status-feed)");
                using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
                if (!resp.IsSuccessStatusCode) return status;
                var xml = await resp.Content.ReadAsStringAsync(ct);
                var doc = XDocument.Parse(xml);
                var firstItem = doc.Descendants("item").FirstOrDefault();
                if (firstItem != null)
                {
                    status.IncidentTitle = firstItem.Element("title")?.Value?.Trim() ?? "";
                    status.IncidentDate  = firstItem.Element("pubDate")?.Value?.Trim() ?? "";
                    var desc = firstItem.Element("description")?.Value ?? "";
                    status.IncidentBody = StripTags(desc).Trim();
                }
            }
            catch
            {
            }
            return status;
        }

        private static string StripTags(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            bool inTag = false;
            foreach (var ch in s)
            {
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; sb.Append(' '); continue; }
                if (!inTag) sb.Append(ch);
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ");
        }
    }
}
