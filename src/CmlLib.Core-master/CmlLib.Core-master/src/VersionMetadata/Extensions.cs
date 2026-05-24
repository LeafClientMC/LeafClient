#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620
namespace CmlLib.Core.VersionMetadata;

public static class Extensions
{
    public static MVersionType GetVersionType(this IVersionMetadata version)
    {
        if (string.IsNullOrEmpty(version.Type))
            return MVersionType.Custom;
        else
            return MVersionTypeConverter.Parse(version.Type);
    }
}