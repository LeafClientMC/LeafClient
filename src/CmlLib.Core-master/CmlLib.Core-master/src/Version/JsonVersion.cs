using System.Text.Json;
using CmlLib.Core.Files;
using CmlLib.Core.Java;
using CmlLib.Core.ProcessBuilder;

namespace CmlLib.Core.Version;

public class JsonVersion : IVersion, IDisposable
{
    private readonly JsonVersionParserOptions _options;
    private readonly JsonDocument _json;
    private readonly JsonVersionDTO _model;

    public JsonVersion(JsonDocument jsonDocument, JsonVersionDTO dto, JsonVersionParserOptions options)
    {
        _options = options;
        _json = jsonDocument;
        _model = dto;
        Id = _model.Id ?? throw new ArgumentException("Null Id");
        // In JsonVersion constructor, add:
        Console.WriteLine($"[JsonVersion DEBUG] Created with side: {_options.Side}");
    }

    public string Id { get; }
    public string MainJarId => getJarId();

    public string? InheritsFrom => _model.InheritsFrom;

    public IVersion? ParentVersion { get; set; }

    private AssetMetadata? _assetIndex;
    public AssetMetadata? AssetIndex => _assetIndex ??= getAssetIndex();

    private MFileMetadata? _client = null;
    public MFileMetadata? Client => _client ??= getClient();

    public JavaVersion? JavaVersion => _model.JavaVersion;

    private IReadOnlyCollection<MLibrary>? _libs = null;
    public IReadOnlyCollection<MLibrary> Libraries => _libs ??= getLibraries();

    public string? Jar => _model.Jar;

    private MLogFileMetadata? _logging;
    public MLogFileMetadata? Logging => _logging ??= getLogging();

    public string? MainClass => _model.MainClass;

    public string? MinecraftArguments => _model.MinecraftArguments;

    public DateTimeOffset ReleaseTime => _model.ReleaseTime;

    public string? Type => _model.Type;

    private IReadOnlyCollection<MArgument>? _gameArgs = null;
    private IReadOnlyCollection<MArgument>? _gameArgsForBase = null;
    public IReadOnlyCollection<MArgument> GetGameArguments(bool isBaseVersion)
    {
        if (isBaseVersion)
            return _gameArgsForBase ??= getGameArguments(true);
        else
            return _gameArgs ??= getGameArguments(false);
    }

    private IReadOnlyCollection<MArgument>? _jvmArgs = null;
    private IReadOnlyCollection<MArgument>? _jvmArgsForBase = null;
    public IReadOnlyCollection<MArgument> GetJvmArguments(bool isBaseVersion)
    {
        if (isBaseVersion)
            return _jvmArgsForBase ??= getJvmArguments(true);
        else
            return _jvmArgs ?? getJvmArguments(false);
    }

    private string getJarId()
    {
        var jar = this.GetInheritedProperty(v => v.Jar);
        if (string.IsNullOrEmpty(jar))
        {
            // If this version inherits from another version, use the parent's MainJarId
            // This fixes Fabric profiles that don't have their own JAR but inherit from base Minecraft
            var parentVersion = this.GetInheritedProperty(v => v.ParentVersion);
            if (parentVersion != null && !string.IsNullOrEmpty(parentVersion.MainJarId))
            {
                return parentVersion.MainJarId;
            }
            return Id;
        }
        else
        {
            return jar;
        }
    }

    private AssetMetadata? getAssetIndex()
    {
        if (string.IsNullOrEmpty(_model.AssetIndex?.Id))
        {
            if (string.IsNullOrEmpty(_model.Assets))
                return null;
            else
                return new AssetMetadata() { Id = _model.Assets };
        }
        else
            return _model.AssetIndex;
    }

    private MFileMetadata? getClient()
    {
        try
        {
            var downloadsElement = _json.RootElement.GetProperty("downloads");
            var sideElement = downloadsElement.GetProperty(_options.Side);

            // MANUALLY DESERIALIZE to avoid reflection issues in AOT
            var client = new MFileMetadata();

            if (sideElement.TryGetProperty("url", out var urlElement))
                client.Url = urlElement.GetString();

            if (sideElement.TryGetProperty("sha1", out var sha1Element))
                client.Sha1 = sha1Element.GetString();

            if (sideElement.TryGetProperty("size", out var sizeElement))
                client.Size = sizeElement.GetInt64();

            return client;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (Exception)
        {
            if (!_options.SkipError)
                throw;
            return null;
        }
    }

    private IReadOnlyCollection<MLibrary> getLibraries()
    {
        try
        {
            Console.WriteLine("[JsonVersion DEBUG] Attempting to get 'libraries' property.");

            if (!_json.RootElement.TryGetProperty("libraries", out var libProp))
            {
                Console.WriteLine("[JsonVersion DEBUG] 'libraries' property NOT found in JSON root.");
                return []; // Return empty if property doesn't exist
            }

            Console.WriteLine($"[JsonVersion DEBUG] 'libraries' property found. ValueKind: {libProp.ValueKind}.");

            if (libProp.ValueKind != JsonValueKind.Array)
            {
                Console.WriteLine($"[JsonVersion DEBUG] 'libraries' property is not an array (ValueKind: {libProp.ValueKind}). Returning empty list.");
                return [];
            }

            var libList = new List<MLibrary>();
            int processedCount = 0;
            foreach (var libJson in libProp.EnumerateArray())
            {
                processedCount++;
                try
                {
                    // Console.WriteLine($"[JsonVersion DEBUG] Processing library item {processedCount}. Raw JSON: {libJson.ToString()}"); // Uncomment for extreme verbosity
                    var lib = JsonLibraryParser.Parse(libJson);
                    if (lib != null)
                    {
                        libList.Add(lib);
                        // Console.WriteLine($"[JsonVersion DEBUG] Successfully parsed library: {lib.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"[JsonVersion DEBUG] JsonLibraryParser.Parse returned NULL for item {processedCount}. Raw JSON: {libJson.ToString()}");
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"[JsonVersion ERROR] Failed to parse library item {processedCount}: {parseEx.Message}. Raw JSON: {libJson.ToString()}");
                }
            }
            Console.WriteLine($"[JsonVersion DEBUG] Finished processing 'libraries' array. Successfully parsed {libList.Count} libraries out of {processedCount} entries.");
            return libList;
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine("[JsonVersion ERROR] KeyNotFoundException caught in getLibraries (unexpected with TryGetProperty).");
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JsonVersion FATAL ERROR] Unexpected and unhandled exception in getLibraries: {ex.Message}. StackTrace: {ex.StackTrace}");
            if (!_options.SkipError)
                throw;
            return Array.Empty<MLibrary>();
        }
    }

    private MLogFileMetadata? getLogging()
    {
        try
        {
            var loggingElement = _json.RootElement.GetProperty("logging");
            var sideElement = loggingElement.GetProperty(_options.Side);

            // MANUALLY DESERIALIZE to avoid reflection issues in AOT
            var logging = new MLogFileMetadata();

            if (sideElement.TryGetProperty("argument", out var argElement))
                logging.Argument = argElement.GetString();

            if (sideElement.TryGetProperty("file", out var fileElement))
            {
                logging.LogFile = new MFileMetadata();
                if (fileElement.TryGetProperty("id", out var idElement))
                    logging.LogFile.Id = idElement.GetString();
                if (fileElement.TryGetProperty("sha1", out var sha1Element))
                    logging.LogFile.Sha1 = sha1Element.GetString();
                if (fileElement.TryGetProperty("size", out var sizeElement))
                    logging.LogFile.Size = sizeElement.GetInt64();
                if (fileElement.TryGetProperty("url", out var urlElement))
                    logging.LogFile.Url = urlElement.GetString();
            }

            if (sideElement.TryGetProperty("type", out var typeElement))
                logging.Type = typeElement.GetString();

            return logging;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (Exception)
        {
            if (!_options.SkipError)
                throw;
            return null;
        }
    }

    private IReadOnlyCollection<MArgument> getGameArguments(bool isBaseVersion)
    {
        try
        {
            var prop = _json.RootElement
                .GetProperty("arguments")
                .GetProperty("game");
            var args = JsonArgumentParser.Parse(prop);
            _gameArgs = args;
            _gameArgsForBase = args;
        }
        catch (KeyNotFoundException)
        {
            var args = GetProperty("minecraftArguments");
            if (string.IsNullOrEmpty(args))
            {
                _gameArgs = Array.Empty<MArgument>();
                _gameArgsForBase = Array.Empty<MArgument>();
            }
            else
            {
                _gameArgs = args
                    .Split(' ')
                    .Select(arg => new MArgument(arg))
                    .ToArray();
                _gameArgsForBase = Array.Empty<MArgument>();
            }
        }
        catch (Exception)
        {
            if (!_options.SkipError)
                throw;
            _gameArgs = Array.Empty<MArgument>();
            _gameArgsForBase = Array.Empty<MArgument>();
        }

        return isBaseVersion ? _gameArgsForBase : _gameArgs;
    }

    private IReadOnlyCollection<MArgument> getJvmArguments(bool isBaseVersion)
    {
        try
        {
            var prop = _json.RootElement
                .GetProperty("arguments")
                .GetProperty("jvm");
            var args = JsonArgumentParser.Parse(prop);
            _jvmArgs = args;
            _jvmArgsForBase = args;
        }
        catch (KeyNotFoundException)
        {
            _jvmArgs = Array.Empty<MArgument>();
            _jvmArgsForBase = Array.Empty<MArgument>();
        }
        catch (Exception)
        {
            if (!_options.SkipError)
                throw;
            _jvmArgs = Array.Empty<MArgument>();
            _jvmArgsForBase = Array.Empty<MArgument>();
        }
        return isBaseVersion ? _jvmArgsForBase : _jvmArgs;
    }

    public string? GetProperty(string key)
    {
        if (_json.RootElement.TryGetProperty(key, out var prop))
            return prop.ToString();
        else
            return null;
    }

    public void Dispose()
    {
        _json.Dispose();
    }
}