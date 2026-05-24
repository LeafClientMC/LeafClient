using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class JreManager
    {
        public static void EnsureRuntimeExecutableBits(string minecraftFolder)
        {
            if (OperatingSystem.IsWindows()) return;
            try
            {
                var runtimeRoot = Path.Combine(minecraftFolder, "runtime");
                if (!Directory.Exists(runtimeRoot)) return;

                var binDirs = Directory.EnumerateDirectories(runtimeRoot, "bin", SearchOption.AllDirectories);
                foreach (var binDir in binDirs)
                {
                    foreach (var file in Directory.EnumerateFiles(binDir))
                    {
                        TryChmodPlusX(file);
                    }
                }

                var libDirs = Directory.EnumerateDirectories(runtimeRoot, "lib", SearchOption.AllDirectories);
                foreach (var libDir in libDirs)
                {
                    foreach (var file in Directory.EnumerateFiles(libDir, "*.so*", SearchOption.AllDirectories))
                    {
                        TryChmodPlusX(file);
                    }
                    foreach (var file in Directory.EnumerateFiles(libDir, "*.dylib", SearchOption.AllDirectories))
                    {
                        TryChmodPlusX(file);
                    }
                    foreach (var file in Directory.EnumerateFiles(libDir, "jspawnhelper", SearchOption.AllDirectories))
                    {
                        TryChmodPlusX(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("JreManager", $"EnsureRuntimeExecutableBits failed: {ex.Message}");
            }
        }

        public static void EnsureSpecificJavaExecutable(string javaExecutablePath)
        {
            if (OperatingSystem.IsWindows()) return;
            if (string.IsNullOrEmpty(javaExecutablePath) || !File.Exists(javaExecutablePath)) return;

            TryChmodPlusX(javaExecutablePath);

            try
            {
                var binDir = Path.GetDirectoryName(javaExecutablePath);
                if (!string.IsNullOrEmpty(binDir) && Directory.Exists(binDir))
                {
                    foreach (var file in Directory.EnumerateFiles(binDir))
                    {
                        TryChmodPlusX(file);
                    }
                }
            }
            catch { }
        }

        private static void TryChmodPlusX(string file)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "chmod",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                psi.ArgumentList.Add("+x");
                psi.ArgumentList.Add(file);
                using var p = Process.Start(psi);
                p?.WaitForExit(2000);
            }
            catch { }
        }

        public static string? FindSystemJava()
        {
            try
            {
                var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (!string.IsNullOrEmpty(javaHome))
                {
                    var bin = Path.Combine(javaHome, "bin",
                        OperatingSystem.IsWindows() ? "java.exe" : "java");
                    if (File.Exists(bin)) return bin;
                }

                if (OperatingSystem.IsLinux())
                {
                    foreach (var p in new[] {
                        "/usr/bin/java", "/usr/lib/jvm/default-java/bin/java",
                        "/usr/lib/jvm/temurin-21-jdk-amd64/bin/java",
                        "/usr/lib/jvm/java-21-openjdk-amd64/bin/java",
                        "/snap/bin/java",
                    })
                    {
                        if (File.Exists(p)) return p;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    foreach (var p in new[] {
                        "/Library/Java/JavaVirtualMachines/temurin-21.jdk/Contents/Home/bin/java",
                        "/Library/Java/JavaVirtualMachines/openjdk-21.jdk/Contents/Home/bin/java",
                        "/usr/bin/java",
                    })
                    {
                        if (File.Exists(p)) return p;
                    }
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "/usr/libexec/java_home",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };
                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            var output = proc.StandardOutput.ReadToEnd().Trim();
                            proc.WaitForExit(2000);
                            if (!string.IsNullOrEmpty(output))
                            {
                                var bin = Path.Combine(output, "bin", "java");
                                if (File.Exists(bin)) return bin;
                            }
                        }
                    }
                    catch { }
                }
                else if (OperatingSystem.IsWindows())
                {
                    foreach (var p in new[] {
                        @"C:\Program Files\Eclipse Adoptium\jdk-21\bin\java.exe",
                        @"C:\Program Files\Java\jdk-21\bin\java.exe",
                        @"C:\Program Files\Microsoft\jdk-21.0.0.0-hotspot\bin\java.exe",
                    })
                    {
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("JreManager", $"FindSystemJava failed: {ex.Message}");
            }
            return null;
        }

        public static IReadOnlyList<string> FindRuntimeJavaCandidates(string minecraftFolder)
        {
            var candidates = new List<string>();
            try
            {
                var runtimeRoot = Path.Combine(minecraftFolder, "runtime");
                if (!Directory.Exists(runtimeRoot)) return candidates;

                var exeName = OperatingSystem.IsWindows() ? "java.exe" : "java";
                foreach (var binDir in Directory.EnumerateDirectories(runtimeRoot, "bin", SearchOption.AllDirectories))
                {
                    var candidate = Path.Combine(binDir, exeName);
                    if (File.Exists(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("JreManager", $"FindRuntimeJavaCandidates failed: {ex.Message}");
            }
            return candidates;
        }
    }
}
