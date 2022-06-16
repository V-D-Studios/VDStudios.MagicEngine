namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Internal use only
/// </summary>
public interface IFunctionalComponent
{
    /// <summary>
    /// Internal use only
    /// </summary>
    public void InternalUninstall();

    /// <summary>
    /// Internal use only
    /// </summary>
    public int Index { get; }
}