using System.Collections.Generic;

namespace LeafClient.Models
{
    public class AccountSecrets
    {
        public string? AccessToken { get; set; }
        public string? Xuid { get; set; }
        public string? LeafApiJwt { get; set; }
        public string? LeafApiRefreshToken { get; set; }
    }

    public class SettingsSecrets
    {
        public int Version { get; set; } = 1;
        public string? SessionAccessToken { get; set; }
        public string? SessionXuid { get; set; }
        public string? MicrosoftRefreshToken { get; set; }
        public string? LeafApiJwt { get; set; }
        public string? LeafApiRefreshToken { get; set; }
        public Dictionary<string, AccountSecrets> Accounts { get; set; } = new Dictionary<string, AccountSecrets>();
    }
}
