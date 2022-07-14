using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace VDStudios.MagicEngine.Tooling.AssetsGenerator;

internal static class Helper
{
    public readonly static Regex CleanupRegex = new(@"^\d+|{|}|;|\.|,|!|\n", RegexOptions.Compiled);
    public static string FileToName(string file)
    {
        var f = file;
        while (CleanupRegex.IsMatch(f))
        {
            f = CleanupRegex.Replace(f, "");
            if (f == "")
                throw new InvalidOperationException($"The filename is an invalid C# Member name and must be set explicitly");
        }
        return f;
    }

    public static void PropSet<T>(ref T v, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        v = value;
    }
    public static T PropGet<T>(ref T v, [CallerMemberName] string? caller = null)
        => v ?? throw new InvalidOperationException($"This asset does not have a {caller}");

}
