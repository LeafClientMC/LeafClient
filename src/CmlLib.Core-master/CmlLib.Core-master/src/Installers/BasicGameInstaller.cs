using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CmlLib.Core.Files;
using CmlLib.Core.Internals; // For IOUtil.ComputeFileSHA1 if needed

namespace CmlLib.Core.Installers
{
    public class BasicGameInstaller : GameInstallerBase
    {
        public BasicGameInstaller(HttpClient httpClient) : base(httpClient)
        {
        }

        protected override async ValueTask Install(
            IEnumerable<GameFile> gameFiles,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("[GameInstaller DEBUG] Starting installation");

            // 1) Build the queue and compute total bytes
            var queue = new HashSet<GameFile>(GameFilePathComparer.Default);
            long totalBytes = 0;
            foreach (var gf in gameFiles)
            {
                // No need for extensive logging here, it's done by the debug instrumentation if needed
                // Console.WriteLine($"[GameInstaller DEBUG] Processing file: {gf.Name}"); 
                // Console.WriteLine($"[GameInstaller DEBUG]   - URL: {gf.Url}");
                // Console.WriteLine($"[GameInstaller DEBUG]   - Path: {gf.Path}");
                // Console.WriteLine($"[GameInstaller DEBUG]   - Size: {gf.Size}");

                if (string.IsNullOrEmpty(gf.Url) || string.IsNullOrEmpty(gf.Path))
                {
                    // Console.WriteLine("[GameInstaller DEBUG]   - SKIPPING: Missing URL or Path");
                    continue;
                }

                if (IsExcludedPath(gf.Path))
                {
                    // Console.WriteLine("[GameInstaller DEBUG]   - SKIPPING: Excluded path");
                    continue;
                }

                // Only add to queue if it actually needs an update
                if (!NeedUpdate(gf))
                {
                    // Console.WriteLine($"[GameInstaller DEBUG]   - SKIPPING: {gf.Name} is up-to-date.");
                    continue;
                }

                if (!queue.Add(gf)) // Add after NeedUpdate to avoid duplicate checks for already updated files
                {
                    // Console.WriteLine("[GameInstaller DEBUG]   - SKIPPING: Duplicate (already in queue)");
                    continue;
                }

                totalBytes += gf.Size;
                FireFileProgress(queue.Count, 0, gf.Name, InstallerEventType.Queued);
            }

            Console.WriteLine($"[GameInstaller DEBUG] Total files to download/update: {queue.Count}");

            // 2) Download each file
            long downloadedBytes = 0;
            int completedFiles = 0;

            foreach (var file in queue)
            {
                FireFileProgress(queue.Count, completedFiles, file.Name, InstallerEventType.Downloading); // Use InstallerEventType.Download
                Console.WriteLine($"[GameInstaller DEBUG] Starting download for: {file.Name}");
                    
                // Ensure target directory exists
                var dir = Path.GetDirectoryName(file.Path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Use the protected Download method from GameInstallerBase
                // This method already handles HttpClient, progress, and cancellation.
                await Download(
                    file,
                    new Progress<ByteProgress>(p => FireByteProgress(p)), // Pass a progress object to the base Download method
                    cancellationToken
                );

                downloadedBytes += file.Size; // Assuming Download completes the file

                completedFiles++;
                FireFileProgress(queue.Count, completedFiles, file.Name, InstallerEventType.Completed);
                Console.WriteLine($"[GameInstaller DEBUG] Completed: {file.Name}");
            }

            Console.WriteLine("[GameInstaller DEBUG] All files downloaded");
        }
    }
}