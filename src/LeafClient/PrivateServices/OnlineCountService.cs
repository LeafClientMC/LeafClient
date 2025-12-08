using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Models; // Make sure this namespace is correct
using LeafClient.Services; // Make sure this namespace is correct

namespace LeafClient.PrivateServices
{
    public class OnlineCountService
    {
        private readonly string _repoOwner = "PlockTheBoost";
        private readonly string _repoName = "plocktheboost.github.io";
        private const string _filePath = "playercount.json";
        private const string _branch = "main";
        private readonly string _encryptedToken = @"fiA6Y8guxAgqKSZzlqT3KlhaRz1KOmnFOeN7mOg3rI8bInP210Kwlj2AK3Msl/VR";

        private readonly HttpClient _client;

        public OnlineCountService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LeafClient", "1.0"));
            _client.Timeout = TimeSpan.FromSeconds(10);

            string decryptedToken = EncryptionService.Decrypt(_encryptedToken);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", decryptedToken);
        }

        public async Task UpdateCount(bool isUserOnline, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[OnlineCountService] Attempting to update online count. User online: {isUserOnline}");
            try
            {
                string getFileUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/contents/{_filePath}?ref={_branch}";
                HttpResponseMessage getResponse = await _client.GetAsync(getFileUrl, cancellationToken);

                GitHubFileContent? fileContent = null;
                if (getResponse.IsSuccessStatusCode)
                {
                    string getFileJson = await getResponse.Content.ReadAsStringAsync(cancellationToken);
                    fileContent = JsonSerializer.Deserialize(getFileJson, JsonContext.Default.GitHubFileContent);
                }
                else if (getResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    string errorContent = await getResponse.Content.ReadAsStringAsync(cancellationToken);
                    Console.Error.WriteLine($"[OnlineCountService ERROR] Failed to get file content (Status: {getResponse.StatusCode}): {errorContent}");
                    return;
                }

                OnlineCountData currentCountData;
                if (fileContent != null && !string.IsNullOrEmpty(fileContent.Content))
                {
                    byte[] data = Convert.FromBase64String(fileContent.Content);
                    string currentContentString = Encoding.UTF8.GetString(data);
                    currentCountData = JsonSerializer.Deserialize(currentContentString, JsonContext.Default.OnlineCountData) ?? new OnlineCountData { Count = 0 };
                }
                else
                {
                    currentCountData = new OnlineCountData { Count = 0 };
                }

                if (isUserOnline)
                {
                    currentCountData.Count++;
                }
                else
                {
                    currentCountData.Count = Math.Max(0, currentCountData.Count - 1);
                }

                Console.WriteLine($"[OnlineCountService] Calculated new online count: {currentCountData.Count}");

                string updatedContentString = JsonSerializer.Serialize(currentCountData, JsonContext.Default.OnlineCountData);
                string encodedUpdatedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(updatedContentString));

                GitHubUpdateFileRequest updateRequest = new GitHubUpdateFileRequest
                {
                    Message = $"Update online count to {currentCountData.Count}",
                    Content = encodedUpdatedContent,
                    Sha = fileContent?.Sha,
                    Branch = _branch
                };

                string requestJson = JsonSerializer.Serialize(updateRequest, JsonContext.Default.GitHubUpdateFileRequest);
                StringContent requestBody = new StringContent(requestJson, Encoding.UTF8, "application/json");

                string updateFileUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/contents/{_filePath}";
                HttpResponseMessage putResponse = await _client.PutAsync(updateFileUrl, requestBody, cancellationToken);

                if (!putResponse.IsSuccessStatusCode)
                {
                    string errorContent = await putResponse.Content.ReadAsStringAsync(cancellationToken);
                    Console.Error.WriteLine($"[OnlineCountService ERROR] Failed to update file content (Status: {putResponse.StatusCode}): {errorContent}");
                }
                else
                {
                    Console.WriteLine($"[OnlineCountService] Online count updated successfully to {currentCountData.Count}.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[OnlineCountService ERROR] The update count operation was cancelled (timed out).");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OnlineCountService CRITICAL ERROR] An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<int> GetOnlineCount()
        {
            Console.WriteLine("[OnlineCountService] Fetching current online count.");
            try
            {
                string getFileUrl = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/contents/{_filePath}?ref={_branch}&t={DateTime.UtcNow.Ticks}";
                HttpResponseMessage getResponse = await _client.GetAsync(getFileUrl);

                if (!getResponse.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine($"[OnlineCountService ERROR] Failed to get online count (Status: {getResponse.StatusCode}). Assuming 0.");
                    return 0;
                }

                string getFileJson = await getResponse.Content.ReadAsStringAsync();
                GitHubFileContent? fileContent = JsonSerializer.Deserialize(getFileJson, JsonContext.Default.GitHubFileContent);

                if (fileContent == null || string.IsNullOrEmpty(fileContent.Content))
                {
                    return 0;
                }

                byte[] data = Convert.FromBase64String(fileContent.Content);
                string currentContentString = Encoding.UTF8.GetString(data);

                OnlineCountData? currentCountData = JsonSerializer.Deserialize(currentContentString, JsonContext.Default.OnlineCountData);
                int count = currentCountData?.Count ?? 0;
                Console.WriteLine($"[OnlineCountService] Current online count fetched: {count}");
                return count;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OnlineCountService CRITICAL ERROR] An unexpected error occurred while fetching online count: {ex.Message}");
                return -1;
            }
        }
    }
}