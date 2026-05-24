using System.Collections.Generic;

namespace LeafClient.Services.ModFolderManagement
{
    public sealed record ModMetadata(
        string ModId,
        string DisplayName,
        string Version,
        string FilePath,
        string FileName,
        string? Sha256,
        IReadOnlyList<ModDependency> Depends,
        IReadOnlyList<ModDependency> Breaks,
        IReadOnlyList<ModDependency> Recommends,
        IReadOnlyList<ModDependency> Suggests,
        IReadOnlyList<ModMetadata> JarInJar,
        bool IsParsed,
        string? ParseError
    );

    public sealed record ModDependency(
        string TargetId,
        string VersionRange
    );

    public enum ConflictSeverity
    {
        Blocker,
        Warning,
        Notice
    }

    public sealed record ConflictReport(
        ModMetadata Source,
        string TargetId,
        string Reason,
        string RequiredRange,
        string? InstalledVersion,
        ConflictSeverity Severity
    );
}
