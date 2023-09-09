using SDL2.NET;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Represents a lifetime of a game that can be manually killed, and ends automatically when the given <see cref="Window"/> closes
/// </summary>
/// <remarks>
/// This is the default <see cref="IGameLifetime"/> used
/// </remarks>
public class GameLifeTimeOnWindowCloses : GameLifetime
{
    /// <summary>
    /// Initializes a new instance of <see cref="GameLifeTimeOnWindowCloses"/> with the passed <see cref="Window"/>
    /// </summary>
    /// <param name="window"></param>
    public GameLifeTimeOnWindowCloses(Window window)
    {
        window.Closed += Window_Closed;
    }

    private void Window_Closed(Window sender, TimeSpan timestamp)
    {
        ShouldRun = false;
    }
}