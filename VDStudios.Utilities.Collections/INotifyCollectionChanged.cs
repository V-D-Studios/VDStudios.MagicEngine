using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace VDStudios.Utilities.Collections;

/// <summary>
/// Notifies listeners of dynamic changes, such as when an item is added or removed, or the whole list is cleared
/// </summary>
/// <typeparam name="TCollection">The type of collection represented by this interface</typeparam>
/// <typeparam name="TItem">The type of items held by the collection</typeparam>
public interface INotifyCollectionChanged<TCollection, TItem> where TCollection : ICollection<TItem>
{
    /// <summary>
    /// An event that is fired whenever this collection is changed
    /// </summary>
    public event CollectionChangedEvent<TCollection, TItem>? CollectionChanged;
}

/// <summary>
/// Notifies listeners of dynamic changes, such as when an item is added or removed, or the whole list is cleared
/// </summary>
/// <typeparam name="TItem">The type of items held by the collection</typeparam>
public interface INotifyCollectionChanged<TItem> : INotifyCollectionChanged<ICollection<TItem>, TItem>
{
}

/// <summary>
/// Represents a method that is called whenever an event pertaining a collection changing is fired
/// </summary>
/// <typeparam name="TCollection">The type of the collection that fired the event</typeparam>
/// <typeparam name="TItem">The type of item the collection holds</typeparam>
/// <param name="sender">The object that fired the event</param>
/// <param name="eventArgs">The arguments of the event</param>
public delegate void CollectionChangedEvent<TCollection, TItem>(TCollection sender, CollectionChangedEventArgs<TItem> eventArgs);
