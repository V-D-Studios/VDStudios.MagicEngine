using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Input;

/// <summary>
/// An object that automatically performs actions based on information from <see cref="InputSnapshot"/>s
/// </summary>
public class KeyMapper
{
    /// <summary>
    /// An action to be bound to a <see cref="Scancode"/>
    /// </summary>
    public delegate void KeyMappedAction(KeyEventRecord keyEvent);

    private readonly record struct ActionInfo(bool PerformRepeat, KeyMappedAction Action);

    private readonly Dictionary<Scancode, ActionInfo> actionDict = new();

    /// <summary>
    /// Creates a new object of type <see cref="KeyMapper"/>
    /// </summary>
    public KeyMapper()
    {

    }

    /// <summary>
    /// Registers <paramref name="action"/> to be invoked and executed when this <see cref="KeyMapper"/> <paramref name="code"/> receives an <see cref="InputSnapshot"/> with an event matching <paramref name="code"/>
    /// </summary>
    /// <param name="code">The <see cref="Scancode"/> to bind the action to</param>
    /// <param name="action">The action to perform when <paramref name="code"/> is pressed</param>
    /// <param name="performOnRepeat">If the action should be invoked if the key is in repeat mode (i.e. held down)</param>
    public void RegisterAction(Scancode code, KeyMappedAction action, bool performOnRepeat = false)
        => actionDict[code] = new(performOnRepeat, action);

    /// <summary>
    /// Unbinds an action from <paramref name="code"/>
    /// </summary>
    /// <returns>Returns <see langword="true"/> if an action was found bound to <paramref name="code"/> and was unbound. <see langword="false"/> if no such action was found.</returns>
    public bool RemoveAction(Scancode code)
        => actionDict.Remove(code);

    /// <summary>
    /// Reviews <paramref name="snapshot"/> and performs the bound actions of any matching scancode found in <see cref="InputSnapshot.KeyEvents"/>
    /// </summary>
    public void PerformActions(InputSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            if (actionDict.TryGetValue(snapshot.KeyEvents[i].Scancode, out var info))
                if (snapshot.KeyEvents[i].Repeat && info.PerformRepeat is false)
                    continue;
                else
                    info.Action(snapshot.KeyEvents[i]);
    }
}
