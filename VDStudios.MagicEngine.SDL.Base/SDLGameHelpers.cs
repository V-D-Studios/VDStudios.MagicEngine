using SDL2.NET;

namespace VDStudios.MagicEngine.SDL.Base;

/// <summary>
/// Helper methods to configure and manage a <see cref="Game"/> that uses SDL
/// </summary>
public static class SDLGameHelpers
{
    /// <summary>
    /// Configures the environment surrounding this game
    /// </summary>
    /// <remarks>
    /// Sets SDL hints and default window flags
    /// </remarks>
    public static void ConfigureEnvironment()
    {
        if (OperatingSystem.IsWindows())
            Hints.DisableThreadNaming.IsEnabled = true;
    }
}
