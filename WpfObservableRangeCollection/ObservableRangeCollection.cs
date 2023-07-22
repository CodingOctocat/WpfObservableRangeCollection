using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace CodingNinja.Wpf.ObjectModel;

/// <summary>
/// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
/// <para>
/// Forked from <see href="https://github.com/xamarin/XamarinCommunityToolkit/blob/main/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs">XamarinCommunityToolkit/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs</see>
/// </para>
/// <para>
/// Instead, it is merged from <see href="https://github.com/jamesmontemagno/mvvm-helpers/blob/master/MvvmHelpers/ObservableRangeCollection.cs">mvvm-helpers/MvvmHelpers/ObservableRangeCollection.cs</see>
/// </para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Initializes a new instance of the <seealso cref="ObservableCollection{T}"/> class.
    /// </summary>
    public ObservableRangeCollection() : base()
    { }

    /// <summary>
    /// Initializes a new instance of the <seealso cref="ObservableCollection{T}"/> class that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">collection: The collection from which the elements are copied.</param>
    /// <exception cref="ArgumentNullException">The collection parameter cannot be null.</exception>
    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection)
    { }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <seealso cref="ObservableCollection{T}"/>.
    /// </summary>
    public void AddRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Add)
    {
        if (notificationMode is not NotifyCollectionChangedAction.Add and not NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("Mode must be either Add or Reset for AddRange.", nameof(notificationMode));
        }

        if (collection.TryGetNonEnumeratedCount(out int count) && count == 1)
        {
            Add(collection.First());

            return;
        }

        var changedItems = collection is List<T> list ? list : new List<T>(collection);

        if (changedItems.Count == 1)
        {
            Add(changedItems[0]);

            return;
        }

        CheckReentrancy();

        int startIndex = Count;

        bool itemsAdded = AddArrangeCore(collection);

        if (!itemsAdded)
        {
            return;
        }

        if (notificationMode == NotifyCollectionChangedAction.Reset)
        {
            RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);

            return;
        }

        RaiseChangeNotificationEvents(
            action: NotifyCollectionChangedAction.Add,
            changedItems: changedItems,
            startingIndex: startIndex);
    }

    /// <summary>
    /// Removes the first occurence of each item in the specified collection from <seealso cref="ObservableCollection{T}"/>.
    /// <para>
    /// NOTE: with notificationMode = Remove, removed items starting index is not set because items are not guaranteed to be consecutive.
    /// </para>
    /// </summary>
    public void RemoveRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Reset)
    {
        if (notificationMode is not NotifyCollectionChangedAction.Remove and not NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("Mode must be either Remove or Reset for RemoveRange.", nameof(notificationMode));
        }

        CheckReentrancy();

        if (notificationMode == NotifyCollectionChangedAction.Reset)
        {
            bool raiseEvents = false;

            foreach (var item in collection)
            {
                Items.Remove(item);
                raiseEvents = true;
            }

            if (raiseEvents)
            {
                RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);
            }

            return;
        }

        var changedItems = new List<T>(collection);

        for (int i = 0; i < changedItems.Count; i++)
        {
            if (!Items.Remove(changedItems[i]))
            {
                changedItems.RemoveAt(i); // Can't use a foreach because changedItems is intended to be (carefully) modified
                i--;
            }
        }

        if (changedItems.Count == 0)
        {
            return;
        }

        RaiseChangeNotificationEvents(
            action: NotifyCollectionChangedAction.Remove,
            changedItems: changedItems);
    }

    /// <summary>
    /// Clears the current collection and replaces it with the specified item.
    /// </summary>
    public void Replace(T item)
    {
        ReplaceRange(new T[] { item });
    }

    /// <summary>
    /// Clears the current collection and replaces it with the specified collection.
    /// </summary>
    public void ReplaceRange(IEnumerable<T> collection)
    {
        CheckReentrancy();

        bool previouslyEmpty = Items.Count == 0;

        Items.Clear();

        AddArrangeCore(collection);

        bool currentlyEmpty = Items.Count == 0;

        if (previouslyEmpty && currentlyEmpty)
        {
            return;
        }

        RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);
    }

    private bool AddArrangeCore(IEnumerable<T> collection)
    {
        bool itemAdded = false;

        foreach (var item in collection)
        {
            Items.Add(item);
            itemAdded = true;
        }

        return itemAdded;
    }

    private void RaiseChangeNotificationEvents(NotifyCollectionChangedAction action, List<T>? changedItems = null, int startingIndex = -1)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        if (changedItems == null)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        }
        else
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, changedItems: changedItems, startingIndex: startingIndex));
        }
    }
}
