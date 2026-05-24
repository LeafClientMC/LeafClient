using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeafClient.PrivateServices
{
    public class OnlineCountData
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

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

    public class GitHubUpdateFileResponse
    {
        [JsonPropertyName("content")]
        public GitHubFileContent? Content { get; set; }
        [JsonPropertyName("commit")]
        public GitHubCommit? Commit { get; set; }
    }

    public class GitHubCommit
    {
        [JsonPropertyName("sha")]
        public string? Sha { get; set; }
    }
}
