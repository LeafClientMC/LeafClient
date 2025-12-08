using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public record ModrinthSearchResponse(int total_hits, List<ModrinthProject> hits);

    public record ModrinthProject(
        string project_id,
        string project_type,
        string slug,
        string title,
        string description,
        string icon_url,
        int downloads,
        int follows,
        string author,
        List<string> versions,
        string date_created,
        string date_modified,
        string license,
        string client_side,
        string server_side,
        List<string> categories,
        List<string> loaders
    );

    public record ModrinthVersionDetailed(
        string id,
        string project_id,
        string name,
        string version_number,
        List<ModrinthFile> files,
        [property: JsonPropertyName("game_versions")] List<string> GameVersions,
        List<string> loaders,
        bool featured,
        string status,
        string date_published
    );

    public record ModrinthVersion(
        string id,
        string projectId,
        string name,
        string versionNumber,
        List<ModrinthFile> files,
        [property: JsonPropertyName("game_versions")] List<string> GameVersions,
        List<string> loaders
    );

    public record ModrinthFile(string url, string filename);
}
