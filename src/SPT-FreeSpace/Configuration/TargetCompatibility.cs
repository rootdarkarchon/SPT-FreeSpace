using System;

namespace SPTFreeSpace.Configuration;

internal static class TargetCompatibility
{
    internal const string MinimumSptVersion = "4.1.0";
    internal const string EftFileVersion = "0.16.9.40743";

    internal static bool Validate(
        string? sptVersion,
        int eftMajor, int eftMinor, int eftBuild, int eftRevision,
        out string error)
    {
        if (!Version.TryParse(sptVersion, out Version version) ||
            version.Major != 4 || version.Minor != 1 || version.Build < 0)
        {
            error = $"SPT-FreeSpace disabled: expected SPT 4.1.x, found '{sptVersion}'.";
            return false;
        }

        // Unity's Mono can truncate FileVersionInfo.FileVersion. Compare the
        // numeric fixed-file-info fields so the complete build remains required.
        if (eftMajor != 0 || eftMinor != 16 || eftBuild != 9 || eftRevision != 40743)
        {
            error =
                $"SPT-FreeSpace disabled: expected EFT executable file version {EftFileVersion}, " +
                $"found '{eftMajor}.{eftMinor}.{eftBuild}.{eftRevision}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
