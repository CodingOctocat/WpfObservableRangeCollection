# WpfObservableRangeCollection

Provides ObservableRangeCollection and its WPF version, including AddRange, RemoveRange, Replace/ReplaceRange methods for bulk operation, but only update the notification once.

---

[![NuGet](https://buildstats.info/nuget/WpfObservableRangeCollection?includePreReleases=true)](https://www.nuget.org/packages/WpfObservableRangeCollection/)

---

## Classes
- `ObservableRangeCollection`: Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
  > Forked from [XamarinCommunityToolkit/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs](https://github.com/xamarin/XamarinCommunityToolkit/blob/main/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs)

- `WpfObservableRangeCollection`: Wpf version of ObservableRangeCollection with CollectionView support.
  > Forked from [weitzhandler/wpfobservablerangecollection-cs](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-wpfobservablerangecollection-cs)

## Why WpfObservableRangeCollection?
See [ObservableCollection Doesn't support AddRange method, so I get notified for each item added, besides what about INotifyCollectionChanging? - StackOverflow](https://stackoverflow.com/q/670577/4380178)

I've searched the web for some ObservableCollections that have *Range methods, but they all raise various exceptions(and some strange problems) in certain specific situations:
- System.NotSupportedException: Range actions are not supported.
- System.InvalidOperationException: The "2" index in the collection change event is not valid for collections of size "1".
- More? I'm not sure. I forgot.

If the `NotSupportedException` still occurred, try using `BindingOperations.EnableCollectionSynchronization(IEnumerable, Object)`.

In the end, I chose `XamarinCommunityToolkit/ObservableRangeCollection` and `weitzhandler/WpfObservableRangeCollection` and made slight changes to the code, and finally, I didn't encounter any problems, for now.

## Seealso
- [weitzhandler/RangeObservableCollection.cs - Gist](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091)
  - [My comment](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091?permalink_comment_id=4634920#gistcomment-4634920)
