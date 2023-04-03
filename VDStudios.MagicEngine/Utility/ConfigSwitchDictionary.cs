using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// A concurrent dictionary that switches when its configuration is changed, and can optionally maintain various states for different configurations
/// </summary>
/// <typeparam name="TKey">The type of the key that are in the dictionary</typeparam>
/// <typeparam name="TValue">The type of the values that are in the dictionary</typeparam>
/// <typeparam name="TConfig">The type of configuration for the dictionary</typeparam>
public class ConfigSwitchDictionary<TKey, TValue, TConfig>
    : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    where TConfig : struct, IEquatable<TConfig>
    where TKey : notnull, IEquatable<TKey>
{
    private readonly object sync = new();
    private readonly ConcurrentDictionary<TConfig, ConcurrentDictionary<TKey, TValue>>? ConfigStore;
    private ConcurrentDictionary<TKey, TValue> Dictionary;

    /// <summary>
    /// Represents a method that can create a value for a given key under a certain configuration
    /// </summary>
    /// <param name="sender">The dictionary that is requesting the new value</param>
    /// <param name="config">The current configuration that <paramref name="sender"/> has</param>
    /// <param name="key">The key that will point to this value</param>
    /// <returns>The newly created value</returns>
    public delegate TValue ValueFactory(ConfigSwitchDictionary<TKey, TValue, TConfig> sender, TConfig config, TKey key);

    /// <summary>
    /// Represents a method that can create a value for a given key under a certain configuration
    /// </summary>
    /// <param name="sender">The dictionary that is requesting the new value</param>
    /// <param name="config">The current configuration that <paramref name="sender"/> has</param>
    /// <param name="key">The key that will point to this value</param>
    /// <param name="argument">An optional argument for the method</param>
    /// <returns>The newly created value</returns>
    public delegate TValue ValueFactory<TArg>(ConfigSwitchDictionary<TKey, TValue, TConfig> sender, TConfig config, TKey key, TArg argument);

    /// <summary>
    /// Creates a new object of type <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// </summary>
    /// <param name="config">The initial configuration for this dictionary</param>
    /// <param name="enableCachingDataWhenConfigChanges"><see langword="true"/> if this dictionary should keep a cache for all configurations that it has had. If <see langword="false"/>, then the methods that accept an argument for keeping such a cache will ignore the argument</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConfigSwitchDictionary(TConfig config, bool enableCachingDataWhenConfigChanges = true)
    {
        ConfigStore = enableCachingDataWhenConfigChanges ? new() : null;
        Config = config;
        Dictionary = new();
    }

    /// <summary>
    /// The current configuration for this <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// </summary>
    /// <remarks>
    /// See <see cref="ChangeConfig"/> to set this value
    /// </remarks>
    public TConfig Config { get; private set; }

    /// <summary>
    /// Sets the value of <see cref="Config"/> and updates related info
    /// </summary>
    /// <param name="newConfig">The new configuration file to use</param>
    /// <param name="cachePreviousValues"><see langword="true"/> If this object should store the values it had for the previous configuration in case it's set again.</param>
    public void ChangeConfig(TConfig newConfig, bool cachePreviousValues = false)
    {
        ConcurrentDictionary<TKey, TValue> d;
        var oldConfig = Config;
        if (oldConfig.Equals(newConfig)) return;
        lock (sync)
            d = Dictionary;

        if (ConfigStore is not null)
        {
            Dictionary = ConfigStore.GetOrAdd(newConfig, c => new ConcurrentDictionary<TKey, TValue>());
            Config = newConfig;
            if (cachePreviousValues)
                ConfigStore.TryAdd(oldConfig, d);
        }
    }

    /// <summary>
    /// Adds a key/value pair to the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// if the key does not already exist.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="defaultValue">the value to be added, if the key does not already exist</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference
    /// (Nothing in Visual Basic).</exception>
    /// <exception cref="OverflowException">The dictionary contains too many
    /// elements.</exception>
    /// <returns>The value for the key.  This will be either the existing value for the key if the
    /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    public TValue GetOrAdd(TKey key, TValue defaultValue)
        => Dictionary.GetOrAdd(key, defaultValue);

    /// <summary>
    /// Adds a key/value pair to the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// if the key does not already exist.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="factory">The function used to generate a value for the key</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference
    /// (Nothing in Visual Basic).</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is a null reference
    /// (Nothing in Visual Basic).</exception>
    /// <exception cref="OverflowException">The dictionary contains too many
    /// elements.</exception>
    /// <returns>The value for the key.  This will be either the existing value for the key if the
    /// key is already in the dictionary, or the new value for the key as returned by valueFactory
    /// if the key was not in the dictionary.</returns>
    public TValue GetOrAdd(TKey key, ValueFactory factory)
    {
        if (Dictionary.TryGetValue(key, out var val) is false)
            Dictionary.TryAdd(key, val = factory(this, Config, key));
        return val;
    }

    /// <summary>
    /// Adds a key/value pair to the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// if the key does not already exist.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="factory">The function used to generate a value for the key</param>
    /// <param name="factoryArgument">An argument value to pass into <paramref name="factory"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference
    /// (Nothing in Visual Basic).</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is a null reference
    /// (Nothing in Visual Basic).</exception>
    /// <exception cref="OverflowException">The dictionary contains too many
    /// elements.</exception>
    /// <returns>The value for the key.  This will be either the existing value for the key if the
    /// key is already in the dictionary, or the new value for the key as returned by valueFactory
    /// if the key was not in the dictionary.</returns>
    public TValue GetOrAdd<TArg>(TKey key, ValueFactory<TArg> factory, TArg factoryArgument)
    {
        if (Dictionary.TryGetValue(key, out var val) is false)
            Dictionary.TryAdd(key, val = factory(this, Config, key, factoryArgument));
        return val;
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key from the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">
    /// When this method returns, <paramref name="value"/> contains the object from
    /// the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> with the specified key or the default value of
    /// <typeparamref name="TValue"/>, if the operation failed.
    /// </param>
    /// <returns>true if the key was found in the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference (Nothing in Visual Basic).</exception>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => Dictionary.TryGetValue(key, out value);

    /// <summary>
    /// Determines whether the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.</param>
    /// <returns>true if the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is a null reference (Nothing in Visual Basic).</exception>
    public bool ContainsKey(TKey key)
        => Dictionary.ContainsKey(key);

    /// <summary>
    /// Attempts to add the specified key and value to the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
    /// in Visual Basic) for reference types.</param>
    /// <returns>
    /// true if the key/value pair was added to the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> successfully; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null reference (Nothing in Visual Basic).</exception>
    /// <exception cref="OverflowException">The <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> contains too many elements.</exception>
    public bool TryAdd(TKey key, TValue value)
        => Dictionary.TryAdd(key, value);

    /// <summary>
    /// Removes all keys and values from the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.
    /// </summary>
    public void Clear() 
        => Dictionary.Clear();

    /// <summary>Gets or sets the value associated with the specified key.</summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <value>
    /// The value associated with the specified key. If the specified key is not found, a get operation throws a
    /// <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.
    /// </value>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is a null reference (Nothing in Visual Basic).
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// The property is retrieved and <paramref name="key"/> does not exist in the collection.
    /// </exception>
    public TValue this[TKey key]
    {
        get => Dictionary[key];
        set => Dictionary[key] = value;
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the <see
    /// cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.
    /// </summary>
    /// <exception cref="OverflowException">The dictionary contains too many
    /// elements.</exception>
    /// <value>The number of key/value pairs contained in the <see
    /// cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>.</value>
    /// <remarks>Count has snapshot semantics and represents the number of items in the <see
    /// cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/>
    /// at the moment when Count was accessed.</remarks>
    public int Count => Dictionary.Count;

    /// <summary>
    /// Gets a value that indicates whether the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> is empty.
    /// </summary>
    /// <value>true if the <see cref="ConfigSwitchDictionary{TKey, TValue, TConfig}"/> is empty; otherwise,
    /// false.</value>
    public bool IsEmpty => Dictionary.IsEmpty;

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
    {
        ((IDictionary<TKey, TValue>)Dictionary).Add(key, value);
    }

    bool IDictionary<TKey, TValue>.Remove(TKey key)
    {
        return ((IDictionary<TKey, TValue>)Dictionary).Remove(key);
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => ((IDictionary<TKey, TValue>)Dictionary).Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => ((IDictionary<TKey, TValue>)Dictionary).Values;

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Add(item);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Contains(item);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Remove(item);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)Dictionary).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Dictionary).GetEnumerator();
    }

    void IDictionary.Add(object key, object? value)
    {
        ((IDictionary)Dictionary).Add(key, value);
    }

    bool IDictionary.Contains(object key)
    {
        return ((IDictionary)Dictionary).Contains(key);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)Dictionary).GetEnumerator();
    }

    void IDictionary.Remove(object key)
    {
        ((IDictionary)Dictionary).Remove(key);
    }

    bool IDictionary.IsFixedSize => ((IDictionary)Dictionary).IsFixedSize;

    object? IDictionary.this[object key] { get => ((IDictionary)Dictionary)[key]; set => ((IDictionary)Dictionary)[key] = value; }

    ICollection IDictionary.Keys => ((IDictionary)Dictionary).Keys;

    ICollection IDictionary.Values => ((IDictionary)Dictionary).Values;

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)Dictionary).CopyTo(array, index);
    }

    bool ICollection.IsSynchronized => ((ICollection)Dictionary).IsSynchronized;

    object ICollection.SyncRoot => ((ICollection)Dictionary).SyncRoot;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)Dictionary).Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)Dictionary).Values;

    bool IDictionary.IsReadOnly => false;
}
