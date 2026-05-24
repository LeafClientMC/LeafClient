// File: XboxAuthNet/XboxLive/Responses/XboxAuthResponseHandler.cs
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace XboxAuthNet.XboxLive.Responses
{
    public class XboxAuthResponseHandler
    {
        public async Task<T> HandleResponse<T>(HttpResponseMessage res)
        {
            var resBody = await res.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            try
            {
                res.EnsureSuccessStatusCode();

                var options = XboxAuthNet.JsonConfig.DefaultOptions ?? new JsonSerializerOptions();
                return JsonSerializer.Deserialize<T>(resBody, options)
                    ?? throw new JsonException();
            }
            catch (Exception ex) when (
                ex is JsonException ||
                ex is HttpRequestException)
            {
                try
                {
                    throw XboxAuthException.FromResponseBody(resBody, (int)res.StatusCode);
                }
                catch (FormatException)
                {
                    try
                    {
                        throw XboxAuthException.FromResponseHeaders(res.Headers, (int)res.StatusCode);
                    }
                    catch (FormatException)
                    {
                        throw new XboxAuthException($"{(int)res.StatusCode}: {res.ReasonPhrase}", (int)res.StatusCode);
                    }
                }
            }
        }
    }
}
