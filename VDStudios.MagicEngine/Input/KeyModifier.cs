namespace VDStudios.MagicEngine.Input;

/// <summary>
/// Represents special keys that are to be interpreted to be modifying the behaviour of other keys. Such as alt or ctrl
/// </summary>
/// <remarks>
/// Modeled after SDL
/// </remarks>
[Flags]
public enum KeyModifier
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    None = 0x0000,
    LeftShift = 0x0001,
    RightShift = 0x0002,
    LeftCtrl = 0x0040,
    RightCtrl = 0x0080,
    LeftAlt = 0x0100,
    RightAlt = 0x0200,

    /// <summary>
    /// Windows key, Command in Mac, etc.
    /// </summary>
    LeftGUI = 0x0400,

    /// <summary>
    /// Windows key, Command in Mac, etc.
    /// </summary>
    RightGUI = 0x0800,
    Num = 0x1000,
    Caps = 0x2000,
    Mode = 0x4000,
    Scroll = 0x8000,

    /* These are defines in the SDL headers */
    Ctrl = LeftCtrl | RightCtrl,
    Shift = LeftShift | RightShift,
    Alt = LeftAlt | RightAlt,
    Gui = LeftGUI | RightGUI,

    Reserved = Scroll
}
