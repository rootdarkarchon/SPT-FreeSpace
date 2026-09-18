using System;

namespace SPTFreeSpace.Configuration;

internal static class TargetCompatibility
{
    internal const string MinimumSptVersion = "4.1.0";
    internal const string EftFileVersion = "0.16.9.40743";

    internal static bool Validate(string? sptVersion, string? eftFileVersion, out string error)
    {
        if (!Version.TryParse(sptVersion, out Version version) ||
            version.Major != 4 || version.Minor != 1 || version.Build < 0)
        {
            error = $"SPT-FreeSpace disabled: expected SPT 4.1.x, found '{sptVersion}'.";
            return false;
        }

        if (!string.Equals(eftFileVersion, EftFileVersion, StringComparison.Ordinal))
        {
            error =
                $"SPT-FreeSpace disabled: expected EFT executable file version {EftFileVersion}, " +
                $"found '{eftFileVersion}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
