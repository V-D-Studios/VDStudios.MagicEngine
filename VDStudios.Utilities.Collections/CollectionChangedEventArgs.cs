using System.Collections.Immutable;
using System.Collections.Specialized;

namespace VDStudios.Utilities.Collections;

/// <summary>
/// Arguments for the CollectionChanged event.
/// A collection that supports INotifyCollectionChangedThis raises this event
/// whenever an item is added or removed, or when the contents of the collection
/// changes dramatically.
/// </summary>
public readonly struct CollectionChangedEventArgs<TItem>
{
    private readonly NotifyCollectionChangedAction _action;
    private readonly ImmutableArray<TItem> _newItems;
    private readonly ImmutableArray<TItem> _oldItems;
    private readonly int _newStartingIndex = -1;
    private readonly int _oldStartingIndex = -1;

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a reset change.
    /// </summary>
    /// <param name="action">The action that caused the event (must be Reset).</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action)
    {
        if (action != NotifyCollectionChangedAction.Reset)
            throw new ArgumentException("The action must be Reset for this constructor to be used", nameof(action));
        _newItems = default;
        _oldItems = default;
        _action = action;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
    /// </summary>
    /// <param name="action">The action that caused the event; can only be Reset, Add or Remove action.</param>
    /// <param name="changedItem">The item affected by the change.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, TItem? changedItem) :
        this(action, changedItem, -1)
    { }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a one-item change.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <param name="changedItem">The item affected by the change.</param>
    /// <param name="index">The index where the change occurred.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, TItem? changedItem, int index) : this()
    {
        switch (action)
        {
            case NotifyCollectionChangedAction.Reset:
                if (changedItem != null)
                {
                    throw new ArgumentException("A 'Reset' action requires a null item", nameof(action));
                }
                if (index != -1)
                {
                    throw new ArgumentException("A 'Reset' action requires an index of -1", nameof(action));
                }
                break;

            case NotifyCollectionChangedAction.Add:
                if (changedItem is null) throw new ArgumentException("An 'Add' action requires an item not to be null");
                _newItems = ImmutableArray.Create(changedItem!);
                _newStartingIndex = index;
                break;

            case NotifyCollectionChangedAction.Remove:
                if (changedItem is null) throw new ArgumentException("A 'Remove' action requires an item not to be null");
                _oldItems = ImmutableArray.Create(changedItem!);
                _oldStartingIndex = index;
                break;

            default:
                throw new ArgumentException("This constructor can only be used for a 'Reset', 'Add' or 'Remove' action", nameof(action));
        }

        _action = action;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <param name="changedItems">The items affected by the change.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, ImmutableArray<TItem> changedItems) :
        this(action, changedItems, -1)
    {
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item change (or a reset).
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <param name="changedItems">The items affected by the change.</param>
    /// <param name="startingIndex">The index where the change occurred.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, ImmutableArray<TItem> changedItems, int startingIndex) : this()
    {
        switch (action)
        {
            case NotifyCollectionChangedAction.Reset:
                if (changedItems.IsDefaultOrEmpty is false)
                {
                    throw new ArgumentException("A 'Reset' action requires the item array to be empty", nameof(action));
                }
                if (startingIndex != -1)
                {
                    throw new ArgumentException("A 'Reset' action requires the index to be -1", nameof(action));
                }
                break;

            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                if (changedItems == null)
                {
                    throw new ArgumentNullException(nameof(changedItems));
                }
                if (startingIndex < -1)
                {
                    throw new ArgumentException("Index cannot be negative for an 'Add' or 'Remove' action", nameof(startingIndex));
                }

                if (action == NotifyCollectionChangedAction.Add)
                {
                    _newItems = changedItems;
                    _newStartingIndex = startingIndex;
                }
                else
                {
                    _oldItems = changedItems;
                    _oldStartingIndex = startingIndex;
                }
                break;

            default:
                throw new ArgumentException("This constructor can only be used for 'Reset', 'Add' or 'Remove' actions", nameof(action));
        }

        _action = action;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
    /// </summary>
    /// <param name="action">Can only be a Replace action.</param>
    /// <param name="newItem">The new item replacing the original item.</param>
    /// <param name="oldItem">The original item that is replaced.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, TItem newItem, TItem oldItem) :
        this(action, newItem, oldItem, -1)
    {
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Replace event.
    /// </summary>
    /// <param name="action">Can only be a Replace action.</param>
    /// <param name="newItem">The new item replacing the original item.</param>
    /// <param name="oldItem">The original item that is replaced.</param>
    /// <param name="index">The index of the item being replaced.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, TItem newItem, TItem oldItem, int index)
    {
        if (action != NotifyCollectionChangedAction.Replace)
            throw new ArgumentException("This constructor can only be used for 'Replace' actions", nameof(action));

        ArgumentNullException.ThrowIfNull(newItem);
        ArgumentNullException.ThrowIfNull(oldItem);

        _action = action;
        _newItems = ImmutableArray.Create(newItem);
        _oldItems = ImmutableArray.Create(oldItem);
        _newStartingIndex = _oldStartingIndex = index;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
    /// </summary>
    /// <param name="action">Can only be a Replace action.</param>
    /// <param name="newItems">The new items replacing the original items.</param>
    /// <param name="oldItems">The original items that are replaced.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, ImmutableArray<TItem> newItems, ImmutableArray<TItem> oldItems) :
        this(action, newItems, oldItems, -1)
    {
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Replace event.
    /// </summary>
    /// <param name="action">Can only be a Replace action.</param>
    /// <param name="newItems">The new items replacing the original items.</param>
    /// <param name="oldItems">The original items that are replaced.</param>
    /// <param name="startingIndex">The starting index of the items being replaced.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, ImmutableArray<TItem> newItems, ImmutableArray<TItem> oldItems, int startingIndex)
    {
        if (action != NotifyCollectionChangedAction.Replace)
        {
            throw new ArgumentException("This constructor can only be used for 'Replace' actions", nameof(action));
        }
        if (newItems == null)
        {
            throw new ArgumentNullException(nameof(newItems));
        }
        if (oldItems == null)
        {
            throw new ArgumentNullException(nameof(oldItems));
        }

        _action = action;
        _newItems = newItems;
        _oldItems = oldItems;
        _newStartingIndex = _oldStartingIndex = startingIndex;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a one-item Move event.
    /// </summary>
    /// <param name="action">Can only be a Move action.</param>
    /// <param name="changedItem">The item affected by the change.</param>
    /// <param name="index">The new index for the changed item.</param>
    /// <param name="oldIndex">The old index for the changed item.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, TItem changedItem, int index, int oldIndex)
    {
        if (action != NotifyCollectionChangedAction.Move)
        {
            throw new ArgumentException("This constructor can only be used for 'Move' actions", nameof(action));
        }
        if (index < 0)
        {
            throw new ArgumentException("Index cannot be negative", nameof(index));
        }
        ArgumentNullException.ThrowIfNull(changedItem);

        _action = action;
        _newItems = _oldItems = ImmutableArray.Create(changedItem);
        _newStartingIndex = index;
        _oldStartingIndex = oldIndex;
    }

    /// <summary>
    /// Construct a NotifyCollectionChangedEventArgs that describes a multi-item Move event.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <param name="changedItems">The items affected by the change.</param>
    /// <param name="index">The new index for the changed items.</param>
    /// <param name="oldIndex">The old index for the changed items.</param>
    public CollectionChangedEventArgs(NotifyCollectionChangedAction action, ImmutableArray<TItem> changedItems, int index, int oldIndex)
    {
        if (action != NotifyCollectionChangedAction.Move)
        {
            throw new ArgumentException("This constructor can only be used for 'Move' actions", nameof(action));
        }
        if (index < 0)
        {
            throw new ArgumentException("Index cannot be negative");
        }

        _action = action;
        _newItems = _oldItems = changedItems.Length is not 0 ? changedItems : default;
        _newStartingIndex = index;
        _oldStartingIndex = oldIndex;
    }

    /// <summary>
    /// The action that caused the event.
    /// </summary>
    public NotifyCollectionChangedAction Action => _action;

    /// <summary>
    /// The items affected by the change.
    /// </summary>
    public ImmutableArray<TItem> NewItems => _newItems;

    /// <summary>
    /// The old items affected by the change (for Replace events).
    /// </summary>
    public ImmutableArray<TItem> OldItems => _oldItems;

    /// <summary>
    /// The index where the change occurred.
    /// </summary>
    public int NewStartingIndex => _newStartingIndex;

    /// <summary>
    /// The old index where the change occurred (for Move events).
    /// </summary>
    public int OldStartingIndex => _oldStartingIndex;
}
