using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace CodingNinja.Wpf.ObjectModel;

/// <summary>
/// Forked from <see href="https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-wpfobservablerangecollection-cs"/>
/// <para>see <see href="https://stackoverflow.com/a/670579/4380178"/></para>
/// <para>If the <seealso cref="NotSupportedException"/> still occurred, try using <seealso cref="BindingOperations.EnableCollectionSynchronization"/>.</para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class WpfObservableRangeCollection<T> : ObservableRangeCollection<T>
{
    private class DeferredEventsCollection : List<NotifyCollectionChangedEventArgs>, IDisposable
    {
        private readonly WpfObservableRangeCollection<T> _collection;

        public DeferredEventsCollection(WpfObservableRangeCollection<T> collection)
        {
            _collection = collection;
            _collection._deferredEvents = this;
        }

        public void Dispose()
        {
            _collection._deferredEvents = null;

            var handlers = _collection
              .GetHandlers()
              .ToLookup(h => h.Target is CollectionView);

            foreach (var handler in handlers[false])
            {
                foreach (var e in this)
                {
                    handler(_collection, e);
                }
            }

            foreach (var cv in handlers[true]
              .Select(h => h.Target)
              .Cast<CollectionView>()
              .Distinct())
            {
                cv.Refresh();
            }
        }
    }

    private DeferredEventsCollection? _deferredEvents;

    public WpfObservableRangeCollection()
    { }

    public WpfObservableRangeCollection(IEnumerable<T> collection) : base(collection)
    { }

    public WpfObservableRangeCollection(List<T> list) : base(list)
    { }

    /// <summary>
    /// Raise CollectionChanged event to any listeners.
    /// Properties/methods modifying this ObservableCollection will raise
    /// a collection changed event through this virtual method.
    /// </summary>
    /// <remarks>
    /// When overriding this method, either call its base implementation
    /// or call <seealso cref="ObservableCollection{T}.BlockReentrancy"/> to guard against reentrant collection changes.
    /// </remarks>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (typeof(ObservableRangeCollection<T>).GetField(nameof(_deferredEvents), BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this) is ICollection<NotifyCollectionChangedEventArgs> deferredEvents)
        {
            deferredEvents.Add(e);

            return;
        }

        foreach (var handler in GetHandlers())
        {
            if (WpfObservableRangeCollection<T>.IsRange(e) && handler.Target is CollectionView cv)
            {
                cv.Refresh();
            }
            else
            {
                handler(this, e);
            }
        }
    }

    private static bool IsRange(NotifyCollectionChangedEventArgs e)
    {
        return e.NewItems?.Count > 1 || e.OldItems?.Count > 1;
    }

    private IEnumerable<NotifyCollectionChangedEventHandler> GetHandlers()
    {
        var info = typeof(ObservableCollection<T>).GetField(nameof(CollectionChanged), BindingFlags.Instance | BindingFlags.NonPublic);
        var @event = info?.GetValue(this) as MulticastDelegate;

        return @event?.GetInvocationList()
          .Cast<NotifyCollectionChangedEventHandler>()
          .Distinct()
          ?? Enumerable.Empty<NotifyCollectionChangedEventHandler>();
    }
}
