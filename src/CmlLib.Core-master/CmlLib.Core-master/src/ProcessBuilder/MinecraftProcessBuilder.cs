using CmlLib.Core.Rules;
using CmlLib.Core.Version;
using CmlLib.Core.Internals;
using System.Diagnostics;

namespace CmlLib.Core.ProcessBuilder;

public class MinecraftProcessBuilder
{
    public MinecraftProcessBuilder(
        IRulesEvaluator evaluator, 
        RulesEvaluatorContext context,
        MLaunchOption option)
    {
        option.CheckValid();

        Debug.Assert(option.StartVersion != null);
        Debug.Assert(option.Path != null);

        launchOption = option;
        version = option.StartVersion;
        minecraftPath = option.Path;
        rulesEvaluator = evaluator;
        baseRulesContext = context;
    }

    private readonly IVersion version;
    private readonly IRulesEvaluator rulesEvaluator;
    private readonly RulesEvaluatorContext baseRulesContext;
    private readonly MinecraftPath minecraftPath;
    private readonly MLaunchOption launchOption;
    
    public Process CreateProcess()
    {
        Debug.Assert(!string.IsNullOrEmpty(launchOption.JavaPath));

        var mc = new Process();
        mc.StartInfo.FileName = launchOption.JavaPath;
        mc.StartInfo.Arguments = BuildArguments();
        mc.StartInfo.WorkingDirectory = minecraftPath.BasePath;
        return mc;
    }

    public string BuildArguments()
    {
        var features = baseRulesContext.Features
            .Concat(launchOption.Features)
            .Concat(addFeatures());
            
        var context = baseRulesContext with 
        { 
            Features = new HashSet<string>(features)
        };

        var argDict = buildArgumentDictionary(context);

        var builder = new MinecraftArgumentBuilder(rulesEvaluator, context, argDict);
        addJvmArguments(builder);
        addGameArguments(builder);
        return builder.Build();
    }

    private IEnumerable<string> addFeatures()
    {
        if (launchOption.IsDemo)
        {
            yield return "is_demo_user";
        }

        if (launchOption.ScreenWidth > 0 && 
            launchOption.ScreenHeight > 0)
        {
            yield return "has_custom_resolution";
        }

        if (!string.IsNullOrEmpty(launchOption.QuickPlayPath))
        {
            yield return "has_quick_plays_support";
        }

        if (!string.IsNullOrEmpty(launchOption.QuickPlaySingleplayer))
        {
            yield return "is_quick_play_singleplayer";
        }

        if (!string.IsNullOrEmpty(launchOption.ServerIp))
        {
            yield return "is_quick_play_multiplayer";
        }

        if (!string.IsNullOrEmpty(launchOption.QuickPlayRealms))
        {
            yield return "is_quick_play_realms";
        }
    }

    private IReadOnlyDictionary<string, string?> buildArgumentDictionary(RulesEvaluatorContext context)
    {
        Debug.Assert(launchOption.Session != null);

        var classpaths = getClasspaths(context);
        var classpath = IOUtil.CombinePath(classpaths, launchOption.PathSeparator);
        var assetId = version.GetInheritedProperty(version => version.AssetIndex?.Id) ?? "legacy";
        
        var argDict = new Dictionary<string, string?>
        {
            { "library_directory"  , minecraftPath.Library },
            { "natives_directory"  , launchOption.NativesDirectory },
            { "launcher_name"      , launchOption.GameLauncherName },
            { "launcher_version"   , launchOption.GameLauncherVersion },
            { "classpath_separator", launchOption.PathSeparator },
            { "classpath"          , classpath },

            { "auth_player_name" , launchOption.Session.Username },
            { "version_name"     , version.Id },
            { "game_directory"   , minecraftPath.BasePath },
            { "assets_root"      , minecraftPath.Assets },
            { "assets_index_name", assetId },
            { "auth_uuid"        , launchOption.Session.UUID },
            { "auth_access_token", launchOption.Session.AccessToken },
            { "user_properties"  , launchOption.UserProperties },
            { "auth_xuid"        , launchOption.Session.Xuid ?? "xuid" },
            { "clientid"         , launchOption.ClientId ?? "clientId" },
            { "user_type"        , launchOption.Session.UserType ?? "Mojang" },
            { "game_assets"      , minecraftPath.GetAssetLegacyPath(assetId) },
            { "auth_session"     , launchOption.Session.AccessToken },
            { "version_type"     , launchOption.VersionType ?? version.Type },

            { "resolution_width"     , launchOption.ScreenWidth.ToString() },
            { "resolution_height"    , launchOption.ScreenHeight.ToString() },
            { "quickPlayPath"        , launchOption.QuickPlayPath },
            { "quickPlaySingleplayer", launchOption.QuickPlaySingleplayer },
            { "quickPlayMultiplayer" , createAddress(launchOption.ServerIp, launchOption.ServerPort) },
            { "quickPlayRealms"      , launchOption.QuickPlayRealms }
        };

        if (launchOption.ArgumentDictionary != null)
        {
            foreach (var argument in launchOption.ArgumentDictionary)
            {
                argDict[argument.Key] = argument.Value;
            }
        }

        return argDict;
    }

    private IEnumerable<string> getClasspaths(RulesEvaluatorContext context)
    {
        var libNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Collect candidate MLibrary objects from this version and its parents
        var candidateLibs = version
            .EnumerateToParent()
            .SelectMany(v => v.Libraries ?? Array.Empty<MLibrary>())
            .ToList();

        Console.WriteLine($"[CmlLib DEBUG] Candidate libraries from version tree: {candidateLibs.Count}");

        var libPathCandidates = new List<(MLibrary lib, string reason, string? path)>();

        foreach (var lib in candidateLibs)
        {
            if (lib == null)
            {
                libPathCandidates.Add((lib, "null-lib", null));
                continue;
            }

            // Check artifact presence
            if (lib.Artifact == null)
            {
                libPathCandidates.Add((lib, "skipped: artifact null", null));
                continue;
            }

            // Check rules
            try
            {
                if (!(lib.Rules == null || rulesEvaluator.Match(lib.Rules, context)))
                {
                    libPathCandidates.Add((lib, "skipped: rules did not match", null));
                    continue;
                }
            }
            catch (Exception ex)
            {
                libPathCandidates.Add((lib, $"skipped: rules check threw ({ex.GetType().Name})", null));
                continue;
            }

            // Unique name guard
            string name;
            try
            {
                name = getLibName(lib);
            }
            catch
            {
                name = lib.Name ?? Guid.NewGuid().ToString();
            }

            if (!libNames.Add(name))
            {
                libPathCandidates.Add((lib, "skipped: duplicate by name", null));
                continue;
            }

            // Compute physical library path
            string libPath;
            try
            {
                libPath = Path.Combine(minecraftPath.Library, lib.GetLibraryPath());
                if (!File.Exists(libPath))
                {
                    libPathCandidates.Add((lib, "skipped: file missing", libPath));
                    continue;
                }
            }
            catch (Exception ex)
            {
                libPathCandidates.Add((lib, $"skipped: get path threw ({ex.GetType().Name})", null));
                continue;
            }

            libPathCandidates.Add((lib, "kept", libPath));
        }

        // Show per-lib reasons
        Console.WriteLine($"[CmlLib DEBUG] Library evaluation summary ({libPathCandidates.Count} entries):");
        foreach (var e in libPathCandidates)
        {
            var fileName = e.path is null ? "<no-path>" : System.IO.Path.GetFileName(e.path);
            Console.WriteLine($"[CmlLib DEBUG] {fileName} => {e.reason} (lib.name='{e.lib?.Name ?? "<null>"}')");
        }

        // Build final libPaths from those marked "kept"
        var libPaths = libPathCandidates
            .Where(x => x.reason == "kept" && x.path != null)
            .Select(x => x.path!)
            .ToList();

        // AOT fallback: if none kept, try enumerating version DTO libraries directly (defensive)
        if (!libPaths.Any())
        {
            try
            {
                if (version is CmlLib.Core.Version.JsonVersion jv)
                {
                    var fallback = jv.EnumerateToParent()
                                    .SelectMany(v => v.Libraries ?? Array.Empty<MLibrary>())
                                    .Select(lib =>
                                    {
                                        try { return Path.Combine(minecraftPath.Library, lib.GetLibraryPath()); }
                                        catch { return null; }
                                    })
                                    .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
                                    .Distinct()
                                    .ToList();

                    if (fallback.Any())
                    {
                        Console.WriteLine($"[CmlLib DEBUG] AOT fallback found {fallback.Count} library files");
                        libPaths.AddRange(fallback);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CmlLib DEBUG] AOT fallback threw: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Add the version JAR (always last in initial collection; we'll reorder for Fabric later)
        var versionJarPath = minecraftPath.GetVersionJarPath(version.MainJarId);
        if (File.Exists(versionJarPath))
        {
            libPaths.Add(versionJarPath);
        }
        else
        {
            Console.WriteLine($"[CmlLib DEBUG] Version jar missing at expected path: {versionJarPath}");
        }

        var finalPaths = libPaths.ToList();

        // Reordering to ensure ASM+mixins appear before Fabric loader (if present).
        var ordered = new List<string>();

        ordered.AddRange(finalPaths.Where(p =>
        {
            var fn = Path.GetFileName(p);
            return fn.StartsWith("asm-", StringComparison.OrdinalIgnoreCase)
                || fn.IndexOf("sponge-mixin", StringComparison.OrdinalIgnoreCase) >= 0
                || fn.IndexOf("mixin", StringComparison.OrdinalIgnoreCase) >= 0; // be permissive
        }));

        ordered.AddRange(finalPaths.Where(p =>
        {
            var fn = Path.GetFileName(p);
            return !(fn.StartsWith("asm-", StringComparison.OrdinalIgnoreCase)
                  || fn.IndexOf("sponge-mixin", StringComparison.OrdinalIgnoreCase) >= 0
                  || fn.IndexOf("mixin", StringComparison.OrdinalIgnoreCase) >= 0
                  || fn.StartsWith("fabric-loader-", StringComparison.OrdinalIgnoreCase));
        }));

        ordered.AddRange(finalPaths.Where(p => Path.GetFileName(p).StartsWith("fabric-loader-", StringComparison.OrdinalIgnoreCase)));

        finalPaths = ordered;

        // Final debug
        Console.WriteLine($"[CmlLib DEBUG] Final classpath items: {finalPaths.Count}");
        foreach (var path in finalPaths)
            Console.WriteLine($"[CmlLib DEBUG] Classpath: {System.IO.Path.GetFileName(path)}");

        return finalPaths;

        static string getLibName(MLibrary lib)
        {
            try
            {
                var package = PackageName.Parse(lib.Name);
                return package.GetIdentifier();
            }
            catch
            {
                return lib.Name ?? Guid.NewGuid().ToString();
            }
        }
    }

    private string? createAddress(string? ip, int port)
    {
        if (port == MinecraftArgumentBuilder.DefaultServerPort)
            return ip;
        else
            return ip + ":" + port;
    }

    private void addJvmArguments(MinecraftArgumentBuilder builder)
    {
        if (launchOption.JvmArgumentOverrides != null)
        {
            // override all jvm arguments
            // even if necessary arguments are missing (-cp, -Djava.library.path),
            // the builder will still add the necessary arguments
            builder.AddArguments(launchOption.JvmArgumentOverrides);
        }
        else
        {
            // version-specific jvm arguments
            var jvmArgs = version.ConcatInheritedJvmArguments().ToList();
            if (jvmArgs.Any())
                builder.AddArguments(jvmArgs);
            else
                builder.AddArguments(MLaunchOption.DefaultJvmArguments);

            // add extra jvm arguments
            builder.AddArguments(launchOption.ExtraJvmArguments);
        }

        // native library
        builder.TryAddNativesDirectory();

        // classpath
        builder.TryAddClassPath();

        // -Xmx
        if (launchOption.MaximumRamMb > 0)
            builder.TryAddXmx(launchOption.MaximumRamMb);

        // -Xms
        if (launchOption.MinimumRamMb > 0)
            builder.TryAddXms(launchOption.MinimumRamMb);
            
        // for macOS
        if (!string.IsNullOrEmpty(launchOption.DockName))
            builder.TryAddDockName(launchOption.DockName);
        if (!string.IsNullOrEmpty(launchOption.DockIcon))
            builder.TryAddDockIcon(launchOption.DockIcon);

        // logging
        var logging = version.GetInheritedProperty(v => v.Logging);
        if (!string.IsNullOrEmpty(logging?.Argument))
        {
            var logArguments = MArgument.FromCommandLine(logging.Argument);
            builder.AddArguments([logArguments], new Dictionary<string, string?>()
            {
                { "path", minecraftPath.GetLogConfigFilePath(logging.LogFile?.Id ?? version.Id) }
            });
        }

        // main class
        var mainClass = version.GetInheritedProperty(v => v.MainClass);
        if (!string.IsNullOrEmpty(mainClass))
            builder.AddArguments([mainClass]);
    }

    private void addGameArguments(MinecraftArgumentBuilder builder)
    {
        // game arguments
        builder.AddArguments(version.ConcatInheritedGameArguments());

        // add extra game arguments
        builder.AddArguments(launchOption.ExtraGameArguments);

        // demo
        if (launchOption.IsDemo)
            builder.SetDemo();

        // screen size
        if (launchOption.ScreenWidth > 0 && launchOption.ScreenHeight > 0)
            builder.TryAddScreenResolution(launchOption.ScreenWidth, launchOption.ScreenHeight);

        // quickPlayMultiplayer
        if (!string.IsNullOrEmpty(launchOption.ServerIp))
            builder.TryAddQuickPlayMultiplayer(launchOption.ServerIp, launchOption.ServerPort);

        // fullscreen
        if (launchOption.FullScreen)
            builder.SetFullscreen();
    }
}
