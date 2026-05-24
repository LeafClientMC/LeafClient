using LeafClient.Models;

namespace LeafClient.Services.ModFolderManagement
{
    public enum JarOwnership
    {
        Managed,
        User,
        Unknown
    }

    public sealed record TrackedJar(
        string FullPath,
        string FileName,
        string? Sha256,
        long SizeBytes,
        JarOwnership Ownership,
        InstalledMod? ManagedEntry
    );
}
