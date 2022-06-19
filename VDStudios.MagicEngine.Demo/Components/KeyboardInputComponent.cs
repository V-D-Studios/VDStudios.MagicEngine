﻿using SDL2.NET;
using SDL2.NET.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.Collections;

namespace VDStudios.MagicEngine.Demo.Components;
public class KeyboardInputComponent<T> : NonUpdatingFunctionalComponent
{
    private readonly Dictionary<Scancode, T> KeyMaps = new();
    private readonly Dictionary<Scancode, Scancode> KeySynonyms = new();
    private readonly LimitedMemberQueue<KeyPressRecord> KeyPressQueue;
    private readonly TimeSpan Timeout;

    public KeyboardInputComponent() : this(10, TimeSpan.FromSeconds(1)) { }

    public KeyboardInputComponent(int queueSize, TimeSpan timeout) : base(false)
    {
        Timeout = timeout;
        Game.MainWindow.KeyPressed += MainWindow_KeyPressed;
        KeyPressQueue = new(queueSize);
    }

    public bool NextKeyPress([NotNullWhen(true)] out KeyPressRecord keyPressRecord)
    {
        while (KeyPressQueue.Count > 0)
            if ((keyPressRecord = KeyPressQueue.Dequeue()).TimeStamp - Game.TotalTime < Timeout)
                return true;

        keyPressRecord = default;
        return false;
    }

    /// <summary>
    /// Maps the given key to the value in question
    /// </summary>
    public KeyboardInputComponent<T> MapKey(Scancode key, T value)
    {
        if (KeySynonyms.TryGetValue(key, out var def))
            throw new InvalidOperationException($"Key {key} is already registered as a synonym of {def}");
        KeyMaps[key] = value;
        return this;
    }

    /// <summary>
    /// Makes <paramref name="synonym"/> represent the same value as <paramref name="key"/>, even if the value represented by <see cref="key"/> changes
    /// </summary>
    public KeyboardInputComponent<T> AddKeySynonym(Scancode key, Scancode synonym)
    {
        if (KeyMaps.ContainsKey(synonym))
            throw new InvalidOperationException($"Key {synonym} is already mapped to a value");
        if (!KeyMaps.ContainsKey(key))
            throw new InvalidOperationException($"Key {key} is not mapped");
        KeySynonyms[synonym] = key;
        return this;
    }

    /// <summary>
    /// Removes this key from the mapping and any synonyms it may have
    /// </summary>
    /// <param name="key"></param>
    public KeyboardInputComponent<T> UnmapKey(Scancode key)
    {
        if (KeyMaps.Remove(key))
            foreach (var k in KeySynonyms.Where(x => x.Value == key).Select(x => x.Key))
                KeySynonyms.Remove(k);
        return this;
    }

    private void MainWindow_KeyPressed(Window sender, TimeSpan timestamp, Scancode scancode, Keycode key, KeyModifier modifiers, bool isPressed, bool repeat, uint unicode)
    {
        if (KeyMaps.TryGetValue(scancode, out var value) || KeySynonyms.TryGetValue(scancode, out var syn) && KeyMaps.TryGetValue(syn, out value)) 
            KeyPressQueue.Enqueue(new(value, timestamp, scancode, key, modifiers, repeat));
    }

    public readonly record struct KeyPressRecord(T Value, TimeSpan TimeStamp, Scancode Scancode, Keycode Key, KeyModifier Modifiers, bool Repeat);
}
