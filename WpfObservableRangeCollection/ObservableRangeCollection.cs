using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace CodingNinja.Wpf.ObjectModel;

/// <summary>
/// <see cref="ObservableCollection{T}"/> that supports bulk operations to avoid frequent update notification events.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    //------------------------------------------------------
    //
    //  Private Fields
    //
    //------------------------------------------------------

    #region Private Fields

    [NonSerialized]
    private DeferredEventsCollection? _deferredEvents;

    #endregion Private Fields

    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="ObservableRangeCollection{T}"/> that is empty and has default initial capacity.
    /// </summary>
    /// <param name="allowDuplicates">Whether duplicate items are allowed in the collection.</param>
    /// <param name="comparer">Supports for <see cref="AllowDuplicates"/>.</param>
    public ObservableRangeCollection(bool allowDuplicates = true, EqualityComparer<T>? comparer = null)
    {
        AllowDuplicates = allowDuplicates;
        Comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableRangeCollection{T}"/> class that contains
    /// elements copied from the specified collection and has sufficient capacity
    /// to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    /// <param name="allowDuplicates">Whether duplicate items are allowed in the collection.</param>
    /// <param name="comparer">Supports for <see cref="AllowDuplicates"/>.</param>
    /// <remarks>
    /// The elements are copied onto the <see cref="ObservableRangeCollection{T}"/> in the
    /// same order they are read by the enumerator of the collection.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference.</exception>
    public ObservableRangeCollection(IEnumerable<T> collection, bool allowDuplicates = true, EqualityComparer<T>? comparer = null) : base(collection)
    {
        AllowDuplicates = allowDuplicates;
        Comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableRangeCollection{T}"/> class
    /// that contains elements copied from the specified list.
    /// </summary>
    /// <param name="list">The list whose elements are copied to the new list.</param>
    /// <param name="allowDuplicates">Whether duplicate items are allowed in the collection.</param>
    /// <param name="comparer">Supports for <see cref="AllowDuplicates"/>.</param>
    /// <remarks>
    /// The elements are copied onto the <see cref="ObservableRangeCollection{T}"/> in the
    /// same order they are read by the enumerator of the list.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is a null reference.</exception>
    public ObservableRangeCollection(List<T> list, bool allowDuplicates = true, EqualityComparer<T>? comparer = null) : base(list)
    {
        AllowDuplicates = allowDuplicates;
        Comparer = comparer ?? EqualityComparer<T>.Default;
    }

    #endregion Constructors

    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether this collection acts as a <see cref="HashSet{T}"/>,
    /// disallowing duplicate items, based on <see cref="Comparer"/>.
    /// This might indeed consume background performance, but in the other hand,
    /// it will pay off in UI performance as less required UI updates are required.
    /// </summary>
    public bool AllowDuplicates { get; set; } = true;

    /// <summary>
    /// Supports for <see cref="AllowDuplicates"/>.
    /// </summary>
    public EqualityComparer<T> Comparer { get; }

    #endregion Public Properties

    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    #region Public Methods

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="ObservableCollection{T}"/>.
    /// </summary>
    /// <param name="collection">
    /// The collection whose elements should be added to the end of the <see cref="ObservableCollection{T}"/>.
    /// The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.
    /// </param>
    /// <returns>Returns the number of items successfully added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    public int AddRange(IEnumerable<T> collection)
    {
        return InsertRange(Count, collection);
    }

    /// <summary>
    /// Inserts the elements of a collection into the <see cref="ObservableCollection{T}"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">
    /// The collection whose elements should be inserted into the <see cref="List{T}"/>.
    /// The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.
    /// </param>
    /// <returns>Returns the number of items successfully inserted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not in the collection range.</exception>
    public int InsertRange(int index, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (!AllowDuplicates)
        {
            collection = collection
              .Distinct(Comparer)
              .Where(item => !Items.Contains(item, Comparer));
        }

        int limitedCount = collection.Take(2).Count();

        if (limitedCount == 0)
        {
            return 0;
        }

        if (limitedCount == 1)
        {
            Add(collection.First());

            return 1;
        }

        CheckReentrancy();

        // Items will always be List<T>, see constructors.
        var items = (List<T>)Items;
        items.InsertRange(index, collection);

        OnEssentialPropertiesChanged();

        // changedItems cannot be IEnumerable(lazy type).
        var changedItems = collection.ToList();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems, index));

        return changedItems.Count;
    }

    /// <summary>
    /// Iterates over the collection and removes all items that satisfy the specified match.
    /// </summary>
    /// <remarks>The complexity is O(n).</remarks>
    /// <param name="match">A function to test each element for a condition.</param>
    /// <returns>Returns the number of items successfully removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
    public int RemoveAll(Predicate<T> match)
    {
        return RemoveAll(0, Count, match);
    }

    /// <summary>
    /// Iterates over the specified range within the collection and removes all items that satisfy the specified match.
    /// <para>NOTE: Consecutively matching elements will trigger the <see cref="ObservableCollection{T}.CollectionChanged"/> event at once.</para>
    /// </summary>
    /// <remarks>The complexity is O(n).</remarks>
    /// <param name="index">The index of where to start performing the search.</param>
    /// <param name="count">The number of items to iterate on.</param>
    /// <param name="match">A function to test each element for a condition.</param>
    /// <returns>Returns the number of items successfully removed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is out of range.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
    public int RemoveAll(int index, int count, Predicate<T> match)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (index + count > Count)
        {
            throw new ArgumentException($"{nameof(index)} + {nameof(count)} must be less than or equal to the ObservableCollection.Count.");
        }

        ArgumentNullException.ThrowIfNull(match);

        if (Count == 0)
        {
            return 0;
        }

        List<T>? cluster = null;
        int clusterIndex = -1;
        int removedCount = 0;

        using (BlockReentrancy())
        using (DeferEvents())
        {
            for (int i = 0; i < count; i++, index++)
            {
                var item = Items[index];

                if (match(item))
                {
                    Items.RemoveAt(index);
                    removedCount++;

                    if (clusterIndex == index)
                    {
                        Debug.Assert(cluster is not null);

                        cluster!.Add(item);
                    }
                    else
                    {
                        cluster = new List<T> { item };
                        clusterIndex = index;
                    }

                    index--;
                }
                else if (clusterIndex > -1)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, cluster, clusterIndex));
                    clusterIndex = -1;
                    cluster = null;
                }
            }

            if (clusterIndex > -1)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, cluster, clusterIndex));
            }
        }

        if (removedCount > 0)
        {
            OnEssentialPropertiesChanged();
        }

        return removedCount;
    }

    /// <summary>
    /// Removes the first occurence of each item in the specified collection from the <see cref="ObservableCollection{T}"/>.
    /// <para>NOTE: Removed items starting index is not set because items are not guaranteed to be consecutive.</para>
    /// </summary>
    /// <param name="collection">The items to remove.</param>
    /// <returns>Returns the number of items successfully removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    public int RemoveRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (Count == 0)
        {
            return 0;
        }

        int limitedCount = collection.Take(2).Count();

        if (limitedCount == 0)
        {
            return 0;
        }

        if (limitedCount == 1)
        {
            bool removed = Remove(collection.First());

            return removed ? 1 : 0;
        }

        CheckReentrancy();

        int removedCount = 0;

        foreach (var item in collection)
        {
            bool removed = Items.Remove(item);
            removedCount += removed ? 1 : 0;
        }

        if (removedCount == 0)
        {
            return 0;
        }

        OnEssentialPropertiesChanged();

        if (Count == 0)
        {
            OnCollectionReset();
        }
        else
        {
            // changedItems cannot be IEnumerable(lazy type).
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, collection.ToList()));
        }

        return removedCount;
    }

    /// <summary>
    /// Removes a range of elements from the <see cref="ObservableCollection{T}"/>.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified range is exceeding the collection.</exception>
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (index + count > Count)
        {
            throw new ArgumentException($"{nameof(index)} + {nameof(count)} must be less than or equal to the ObservableCollection.Count.");
        }

        if (count == 0)
        {
            return;
        }

        if (count == 1)
        {
            RemoveItem(index);

            return;
        }

        if (index == 0 && count == Count)
        {
            Clear();

            return;
        }

        // Items will always be List<T>, see constructors.
        var items = (List<T>)Items;
        var removedItems = items.GetRange(index, count);

        CheckReentrancy();

        items.RemoveRange(index, count);

        OnEssentialPropertiesChanged();

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
    }

    /// <summary>
    /// Clears the current collection and replaces it with the specified item, using <see cref="Comparer"/>.
    /// </summary>
    /// <param name="item">The item to fill the collection with, after clearing it.</param>
    public void Replace(T item)
    {
        ReplaceRange(0, Count, new[] { item });
    }

    /// <summary>
    /// Clears the current collection and replaces it with the specified collection, using <see cref="Comparer"/>.
    /// </summary>
    /// <param name="collection">The items to fill the collection with, after clearing it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    public void ReplaceRange(IEnumerable<T> collection)
    {
        ReplaceRange(0, Count, collection);
    }

    /// <summary>
    /// Removes the specified range and inserts the specified collection in its position, leaving equal items in equal positions intact.
    /// <para>When index and count are equal to 0, it is equivalent to InsertRange(0, collection).</para>
    /// </summary>
    /// <remarks>This method is roughly equivalent to <see cref="RemoveRange(Int32, Int32)"/> then <see cref="InsertRange(Int32, IEnumerable{T})"/>.</remarks>
    /// <param name="index">The index of where to start the replacement.</param>
    /// <param name="count">The number of items to be replaced.</param>
    /// <param name="collection">The collection to insert in that location.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is out of range.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><see cref="Comparer"/> is null.</exception>
    public void ReplaceRange(int index, int count, IEnumerable<T> collection)
    {
        void OnRangeReplaced(int followingItemIndex, ICollection<T> newCluster, ICollection<T> oldCluster)
        {
            if (oldCluster is null || oldCluster.Count == 0)
            {
                Debug.Assert(newCluster is null || newCluster.Count == 0);

                return;
            }

            OnCollectionChanged(
              new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace,
                new List<T>(newCluster),
                new List<T>(oldCluster),
                followingItemIndex - oldCluster.Count));

            oldCluster.Clear();
            newCluster.Clear();
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (index + count > Count)
        {
            throw new ArgumentException($"{nameof(index)} + {nameof(count)} must be less than or equal to the ObservableCollection.Count.");
        }

        ArgumentNullException.ThrowIfNull(collection);

        if (!collection.Any())
        {
            RemoveRange(index, count);

            return;
        }

        if (!AllowDuplicates)
        {
            collection = collection
              .Distinct(Comparer)
              .ToList();
        }

        if (index + count == 0)
        {
            InsertRange(0, collection);

            return;
        }

        if (collection is not IList<T> list)
        {
            list = new List<T>(collection);
        }

        using (BlockReentrancy())
        using (DeferEvents())
        {
            int rangeCount = index + count;
            int addedCount = list.Count;

            bool changesMade = false;
            List<T>? newCluster = null;
            List<T>? oldCluster = null;

            int i = index;

            for (; i < rangeCount && i - index < addedCount; i++)
            {
                // Parallel position.
                T old = this[i], @new = list[i - index];

                if (Comparer.Equals(old, @new))
                {
                    OnRangeReplaced(i, newCluster!, oldCluster!);

                    continue;
                }
                else
                {
                    Items[i] = @new;

                    if (newCluster is null)
                    {
                        Debug.Assert(oldCluster is null);

                        newCluster = new List<T> { @new };
                        oldCluster = new List<T> { old };
                    }
                    else
                    {
                        newCluster.Add(@new);
                        oldCluster!.Add(old);
                    }

                    changesMade = true;
                }
            }

            OnRangeReplaced(i, newCluster!, oldCluster!);

            // Exceeding position.
            if (count != addedCount)
            {
                // Items will always be List<T>, see constructors.
                var items = (List<T>)Items;

                if (count > addedCount)
                {
                    int removedCount = rangeCount - addedCount;
                    var removed = new T[removedCount];
                    items.CopyTo(i, removed, 0, removed.Length);
                    items.RemoveRange(i, removedCount);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, i));
                }
                else
                {
                    int k = i - index;
                    var added = new T[addedCount - k];

                    for (int j = k; j < addedCount; j++)
                    {
                        var @new = list[j];
                        added[j - k] = @new;
                    }

                    items.InsertRange(i, added);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, i));
                }

                OnEssentialPropertiesChanged();
            }
            else if (changesMade)
            {
                OnIndexerPropertyChanged();
            }
        }
    }

    #endregion Public Methods

    //------------------------------------------------------
    //
    //  Protected Methods
    //
    //------------------------------------------------------

    #region Protected Methods

    /// <summary>
    /// Called by base class <see cref="Collection{T}"/> when the list is being cleared;
    /// raises a <see cref="ObservableCollection{T}.CollectionChanged"/> event to any listeners.
    /// </summary>
    protected override void ClearItems()
    {
        if (Count == 0)
        {
            return;
        }

        base.ClearItems();
    }

    /// <summary>
    /// Create a new <see cref="DeferredEventsCollection"/>(<see langword="this"/>) instance.
    /// </summary>
    /// <returns></returns>
    protected virtual IDisposable DeferEvents()
    {
        return new DeferredEventsCollection(this);
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        if (!AllowDuplicates && Items.Contains(item))
        {
            return;
        }

        base.InsertItem(index, item);
    }

    /// <summary>
    /// Raise <see cref="ObservableCollection{T}.CollectionChanged"/> event to any listeners.
    /// Properties/methods modifying this <see cref="ObservableCollection{T}"/> will raise
    /// a collection changed event through this virtual method.
    /// </summary>
    /// <remarks>
    /// When overriding this method, either call its base implementation
    /// or call <see cref="ObservableCollection{T}.BlockReentrancy"/> to guard against reentrant collection changes.
    /// </remarks>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_deferredEvents is not null)
        {
            _deferredEvents.Add(e);

            return;
        }

        base.OnCollectionChanged(e);
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, T item)
    {
        if (AllowDuplicates)
        {
            if (Comparer.Equals(this[index], item))
            {
                return;
            }
        }
        else if (Items.Contains(item, Comparer))
        {
            return;
        }

        base.SetItem(index, item);
    }

    #endregion Protected Methods

    //------------------------------------------------------
    //
    //  Private Methods
    //
    //------------------------------------------------------

    #region Private Methods

    /// <summary>
    /// Helper to raise CollectionChanged event with action == Reset to any listeners.
    /// </summary>
    private void OnCollectionReset()
    {
        OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
    }

    /// <summary>
    /// Helper to raise Count property and the Indexer property.
    /// </summary>
    private void OnEssentialPropertiesChanged()
    {
        OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        OnIndexerPropertyChanged();
    }

    /// <summary>
    /// Helper to raise a PropertyChanged event for the Indexer property.
    /// </summary>
    private void OnIndexerPropertyChanged()
    {
        OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
    }

    #endregion Private Methods

    //------------------------------------------------------
    //
    //  Private Types
    //
    //------------------------------------------------------

    #region Private Types

    private sealed class DeferredEventsCollection : List<NotifyCollectionChangedEventArgs>, IDisposable
    {
        private readonly ObservableRangeCollection<T> _collection;

        public DeferredEventsCollection(ObservableRangeCollection<T> collection)
        {
            Debug.Assert(collection is not null);
            Debug.Assert(collection._deferredEvents is null);

            _collection = collection;
            _collection._deferredEvents = this;
        }

        public void Dispose()
        {
            _collection._deferredEvents = null;

            foreach (var args in this)
            {
                _collection.OnCollectionChanged(args);
            }
        }
    }

    #endregion Private Types
}

/// <remarks>
/// To be kept outside <see cref="ObservableCollection{T}"/>, since otherwise, a new instance will be created for each generic type used.
/// </remarks>
internal static class EventArgsCache
{
    internal static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");

    internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");

    internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
}
