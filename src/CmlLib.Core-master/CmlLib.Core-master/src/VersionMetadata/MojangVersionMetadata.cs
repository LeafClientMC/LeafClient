using System.Text.Json;
using CmlLib.Core.Json; // Add this using

namespace CmlLib.Core.VersionMetadata;

public class MojangVersionMetadata : JsonVersionMetadata
{
    private HttpClient _httpClient;

    public string Url { get; }

    public MojangVersionMetadata(JsonVersionMetadataModel model, HttpClient httpClient) : base(model)
    {
        _httpClient = httpClient;
        IsSaved = false;

        if (string.IsNullOrEmpty(model.Url))
            throw new ArgumentNullException(nameof(model.Url));

        Url = model.Url;
    }

    protected override async ValueTask<Stream> GetVersionJsonStream(CancellationToken cancellationToken)
    {
        var res = await _httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStreamAsync();
    }

    // Override the method that deserializes the version JSON if it exists in the base class
    // If there's a method in the base class that does JSON deserialization, you'll need to override it too
}