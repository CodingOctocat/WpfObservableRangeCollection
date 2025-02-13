# WpfObservableRangeCollection

[![NuGet](https://img.shields.io/nuget/dt/WpfObservableRangeCollection)](https://www.nuget.org/packages/WpfObservableRangeCollection/)
[![Target framework](https://img.shields.io/badge/support-.NET_6.0--Windows7.0-blue)](https://github.com/CodingOctocat/WpfObservableRangeCollection)
[![Target framework](https://img.shields.io/badge/support-.NET_8.0--Windows7.0-blue)](https://github.com/CodingOctocat/WpfObservableRangeCollection)
[![GitHub issues](https://img.shields.io/github/issues/CodingOctocat/WpfObservableRangeCollection)](https://github.com/CodingOctocat/WpfObservableRangeCollection/issues)
[![GitHub stars](https://img.shields.io/github/stars/CodingOctocat/WpfObservableRangeCollection)](https://github.com/CodingOctocat/WpfObservableRangeCollection/stargazers)
[![GitHub license](https://img.shields.io/github/license/CodingOctocat/WpfObservableRangeCollection)](https://github.com/CodingOctocat/WpfObservableRangeCollection/blob/master/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/codingoctocat/wpfobservablerangecollection/badge)](https://www.codefactor.io/repository/github/codingoctocat/wpfobservablerangecollection)

Provides ObservableRangeCollection and its WPF version, including AddRange, InsertRange, RemoveRange/RemoveAll, Replace/ReplaceRange methods for bulk operation to avoid frequent update notification events.

## NuGet Package Manager

    PM> Install-Package WpfObservableRangeCollection

## .NET CLI

    dotnet add package WpfObservableRangeCollection

---

## Classes
- `ObservableRangeCollection`: An ObservableCollection that supports bulk operations to avoid frequent update notification events.
  > Forked from [weitzhandler/rangeobservablecollection-cs](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-rangeobservablecollection-cs)

- `WpfObservableRangeCollection`: WPF version of ObservableRangeCollection with CollectionView support.
  > Forked from [weitzhandler/wpfobservablerangecollection-cs](https://gist.github.com/weitzhandler/65ac9113e31d12e697cb58cd92601091#file-wpfobservablerangecollection-cs)

# Usage

```csharp
var collection = new WpfObservableRangeCollection<int>();
collection.AddRange(Enumerable.Range(0,10));
```
> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }

</br>

```csharp
collection.RemoveRange(index: 5, count: 3);
```
> { 0, 1, 2, 3, 4, ~~5, 6, 7~~ 8, 9 }

</br>

```csharp
// You can also receive the return value to get the number of items that were successfully removed.
// removed here is 2.
int removed = collection.RemoveRange(new[] { 1, 3, 5 });
```
> { 0, ~~1~~ 2, ~~3~~ 4, 8, 9 }

</br>

```csharp
collection.InsertRange(index: 2, collection: Enumerable.Range(10, 7));
```
> { 0, 2, 10, 11, 12, 13, 14, 15, 16, 4, 8, 9 }

</br>

```csharp
// This method is roughly equivalent to RemoveRange, then InsertRange.
// When index and count are equal to 0, it is equivalent to InsertRange(0, collection).
// changed here is 0.
int changed = collection.ReplaceRange(index: 6, count: 3, new[] { -1, -2, -3 });
```
> { 0, 2, 10, 11, 12, 13, -1, -2, -3, 4, 8, 9 }

</br>

```csharp
// Clears the current collection and replaces it with the specified item.
collection.Replace(42);
```
> { 42 }

</br>

- If duplicate items are not allowed in the collection, set `AllowDuplicates = false`, and you can specify the `Comparer = xxx`.
- Most of the extended methods have return values to indicate changes in the number of collections.

## Why WpfObservableRangeCollection?
See [ObservableCollection Doesn't support AddRange method, so I get notified for each item added, besides what about INotifyCollectionChanging? - StackOverflow](https://stackoverflow.com/q/670577/4380178)

I've searched the web for some ObservableCollections that have *\*Range* methods, but they all raise various exceptions(and some strange problems) in certain specific situations:
- System.NotSupportedException: Range actions are not supported.
- System.InvalidOperationException: The "x" index in the collection change event is not valid for collections of size "y".
- More? I'm not sure. I forgot.

In the end, I chose `weitzhandler/RangeObservableCollection` and `weitzhandler/WpfObservableRangeCollection` and made slight changes to the code, and finally, I didn't encounter any problems, for now.

> If the `NotSupportedException` still occurred, try using `BindingOperations.EnableCollectionSynchronization(IEnumerable, Object)`.

## Seealso
- [Cysharp/ObservableCollections](https://github.com/Cysharp/ObservableCollections)
- [ENikS/ObservableCollectionEx](https://github.com/ENikS/ObservableCollectionEx)
- [XamarinCommunityToolkit/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs](https://github.com/xamarin/XamarinCommunityToolkit/blob/main/src/CommunityToolkit/Xamarin.CommunityToolkit/ObjectModel/ObservableRangeCollection.shared.cs)
