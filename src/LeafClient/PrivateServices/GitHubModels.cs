using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeafClient.PrivateServices
{
    /// <summary>
    /// Represents the structure of the online_count.json file on GitHub.
    /// </summary>
    public class OnlineCountData
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Represents the response when fetching file content from GitHub API.
    /// </summary>
    public class GitHubFileContent
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("path")]
        public string? Path { get; set; }
        [JsonPropertyName("sha")]
        public string? Sha { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("encoding")]
        public string? Encoding { get; set; }
    }

    /// <summary>
    /// Represents the request body for updating a file on GitHub API.
    /// </summary>
    public class GitHubUpdateFileRequest
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("sha")]
        public string? Sha { get; set; }
        [JsonPropertyName("branch")]
        public string? Branch { get; set; }
    }

    /// <summary>
    /// Represents the response when updating a file on GitHub API.
    /// (Simplified for relevant properties)
    /// </summary>
    public class GitHubUpdateFileResponse
    {
        [JsonPropertyName("content")]
        public GitHubFileContent? Content { get; set; }
        [JsonPropertyName("commit")]
        public GitHubCommit? Commit { get; set; }
    }

    /// <summary>
    /// Represents a GitHub commit object. (Simplified for relevant properties)
    /// </summary>
    public class GitHubCommit
    {
        [JsonPropertyName("sha")]
        public string? Sha { get; set; }
        // Add other commit properties if needed for detailed responses, e.g., author, message.
    }
}
