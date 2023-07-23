# WpfObservableRangeCollection

Provides ObservableRangeCollection and its WPF version, including AddRange, InsertRange, RemoveRange/RemoveAll, Replace/ReplaceRange methods for bulk operation to avoid frequent update notification events.

---

[![NuGet](https://buildstats.info/nuget/WpfObservableRangeCollection?includePreReleases=true)](https://www.nuget.org/packages/WpfObservableRangeCollection/)

---

## Classes
- `ObservableRangeCollection`: Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
  > Forked from [weitzhandler/rangeobservablecollection-cs](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-rangeobservablecollection-cs)

- `WpfObservableRangeCollection`: Wpf version of ObservableRangeCollection with CollectionView support.
  > Forked from [weitzhandler/wpfobservablerangecollection-cs](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-wpfobservablerangecollection-cs)

## Why WpfObservableRangeCollection?
See [ObservableCollection Doesn't support AddRange method, so I get notified for each item added, besides what about INotifyCollectionChanging? - StackOverflow](https://stackoverflow.com/q/670577/4380178)

I've searched the web for some ObservableCollections that have *Range methods, but they all raise various exceptions(and some strange problems) in certain specific situations:
- System.NotSupportedException: Range actions are not supported.
- System.InvalidOperationException: The "x" index in the collection change event is not valid for collections of size "y".
- More? I'm not sure. I forgot.

In the end, I chose `weitzhandler/RangeObservableCollection` and `weitzhandler/WpfObservableRangeCollection` and made slight changes to the code, and finally, I didn't encounter any problems, for now.

> If the `NotSupportedException` still occurred, try using `BindingOperations.EnableCollectionSynchronization(IEnumerable, Object)`.

## Seealso
- [Cysharp/ObservableCollections](https://github.com/Cysharp/ObservableCollections)
- [ENikS/ObservableCollectionEx](https://github.com/ENikS/ObservableCollectionEx)
- [XamarinCommunityToolkit/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs](https://github.com/xamarin/XamarinCommunityToolkit/blob/main/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs)
