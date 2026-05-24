// File: XboxAuthNet/HttpHelper.cs
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web; // Keep this if used for HttpUtility.UrlEncode

namespace XboxAuthNet
{
    public class HttpHelper
    {
        public const string UserAgent =
            "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/71.0.3578.98 Safari/537.36";

        public static string GetQueryString(Dictionary<string, string?> queries)
        {
            return string.Join("&",
                queries.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
        }

        public static HttpContent CreateJsonContent<T>(T obj)
        {
            var options = JsonConfig.DefaultOptions ?? new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null // Explicitly set to null for default PascalCase/camelCase based on PropertyNameCaseInsensitive
            };

            return JsonContent.Create(
                inputValue: obj,
                mediaType: new MediaTypeHeaderValue("application/json"),
                options: options);
        }
    }
}
