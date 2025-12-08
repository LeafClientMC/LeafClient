using CmlLib.Core.Rules;
using CmlLib.Core.Files;
using CmlLib.Core.Version;

namespace CmlLib.Core.FileExtractors;

public class ClientFileExtractor : IFileExtractor
{
    public ValueTask<IEnumerable<GameFile>> Extract(
        MinecraftPath path, 
        IVersion version,
        RulesEvaluatorContext rulesContext,
        CancellationToken cancellationToken)
    {
        var result = extract(path, version);
        return new ValueTask<IEnumerable<GameFile>>(result);
    }

    private IEnumerable<GameFile> extract(MinecraftPath path, IVersion version)
    {
        Console.WriteLine($"[ClientFileExtractor DEBUG] Starting extraction for version: {version.Id}");
        Console.WriteLine($"[ClientFileExtractor DEBUG] MainJarId: {version.MainJarId}");

        // Get the actual client JAR ID from the inheritance chain
        var clientJarId = version.GetInheritedProperty(v => v.MainJarId);
        var client = version.GetInheritedProperty(v => v.Client);

        Console.WriteLine($"[ClientFileExtractor DEBUG] ClientJarId: {clientJarId}");
        Console.WriteLine($"[ClientFileExtractor DEBUG] Client metadata: {client != null}");
        Console.WriteLine($"[ClientFileExtractor DEBUG] Client URL: {client?.Url}");

        if (string.IsNullOrEmpty(client?.Url) || string.IsNullOrEmpty(clientJarId))
        {
            Console.WriteLine($"[ClientFileExtractor DEBUG] Skipping - no client URL or JAR ID");
            yield break;
        }

        var clientPath = path.GetVersionJarPath(clientJarId);
        Console.WriteLine($"[ClientFileExtractor DEBUG] Client path: {clientPath}");

        yield return new GameFile(clientJarId)
        {
            Path = clientPath,
            Url = client.Url,
            Hash = client.GetSha1(),
            Size = client.Size
        };

        Console.WriteLine($"[ClientFileExtractor DEBUG] Added client JAR to download queue");
    }
}
