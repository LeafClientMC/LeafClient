using System.Text.Json;
using XboxAuthNet.XboxLive.Requests;

namespace LeafClient
{
    internal static class Json
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = JsonContext.Default
        };

        static Json()
        {
            XboxAuthNet.JsonConfig.DefaultOptions = Options;
            CmlLib.Core.Auth.Microsoft.JsonConfig.DefaultOptions = Options;
            XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = Options;
        }
    }
}