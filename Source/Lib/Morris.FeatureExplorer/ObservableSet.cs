using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged;

namespace Morris.FeatureExplorer;

/// <summary>
/// A set that raises change notifications compatible with WPF/WinUI data binding.
/// Notifications are per-item (simple and predictable).
/// </summary>
[SuppressPropertyChangedWarnings]
public class ObservableSet<T> :
	ISet<T>,
	ICollection<T>,
	IEnumerable<T>,
	IEnumerable,
	INotifyCollectionChanged,
	INotifyPropertyChanged
	where T : notnull
{
	private const string IndexerName = "Item[]";
	private readonly HashSet<T> Storage;

	public event NotifyCollectionChangedEventHandler CollectionChanged;
	public event PropertyChangedEventHandler PropertyChanged;

	public ObservableSet()
	{
		Storage = new HashSet<T>();
	}

	public ObservableSet(IEqualityComparer<T> comparer)
	{
		Storage = new HashSet<T>(comparer);
	}

	public ObservableSet(IEnumerable<T> items, IEqualityComparer<T> comparer = null)
	{
		Storage = new HashSet<T>(items, comparer ?? EqualityComparer<T>.Default);
	}

	public int Count => Storage.Count;
	public bool IsReadOnly => ((ICollection<T>)Storage).IsReadOnly;

	public IEqualityComparer<T> Comparer => Storage.Comparer;

	public bool Contains(T item) => Storage.Contains(item);

	public bool Add(T item)
	{
		if (Storage.Add(item))
		{
			RaiseAdd(item);
			return true;
		}
		return false;
	}

	void ICollection<T>.Add(T item) => Add(item);

	public bool Remove(T item)
	{
		if (Storage.Remove(item))
		{
			RaiseRemove(item);
			return true;
		}
		return false;
	}

	public void Clear()
	{
		if (Storage.Count == 0)
			return;

		Storage.Clear();
		RaiseReset();
	}

	public void CopyTo(T[] array, int arrayIndex) => Storage.CopyTo(array, arrayIndex);

	public IEnumerator<T> GetEnumerator() => Storage.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void ExceptWith(IEnumerable<T> other)
	{
		if (other is null) return;
		foreach (var item in other)
		{
			if (Storage.Remove(item))
				RaiseRemove(item);
		}
	}

	public void IntersectWith(IEnumerable<T> other)
	{
		if (other is null) return;

		var toKeep = new HashSet<T>(other, Comparer);
		var toRemove = new List<T>();

		foreach (var item in Storage)
			if (!toKeep.Contains(item))
				toRemove.Add(item);

		foreach (var item in toRemove)
		{
			Storage.Remove(item);
			RaiseRemove(item);
		}
	}

	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		if (other is null) return;

		foreach (var item in other)
		{
			if (!Storage.Remove(item))
			{
				Storage.Add(item);
				RaiseAdd(item);
			}
			else
			{
				RaiseRemove(item);
			}
		}
	}

	public void UnionWith(IEnumerable<T> other)
	{
		if (other is null) return;
		foreach (var item in other)
			if (Storage.Add(item))
				RaiseAdd(item);
	}

	public bool IsProperSubsetOf(IEnumerable<T> other) => Storage.IsProperSubsetOf(other);
	public bool IsProperSupersetOf(IEnumerable<T> other) => Storage.IsProperSupersetOf(other);
	public bool IsSubsetOf(IEnumerable<T> other) => Storage.IsSubsetOf(other);
	public bool IsSupersetOf(IEnumerable<T> other) => Storage.IsSupersetOf(other);
	public bool Overlaps(IEnumerable<T> other) => Storage.Overlaps(other);
	public bool SetEquals(IEnumerable<T> other) => Storage.SetEquals(other);

	// ----- Convenience range op (per-item notifications) -----

	public void AddRange(IEnumerable<T> items)
	{
		if (items is null) return;
		foreach (var item in items)
			if (Storage.Add(item))
				RaiseAdd(item);
	}

	// ----- Notification helpers -----

	private void RaiseAdd(T item)
	{
		OnPropertyChanged(nameof(Count));
		OnPropertyChanged(IndexerName);
		RaiseCollectionChangedCore(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Add,
			new[] { item }));
	}

	private void RaiseRemove(T item)
	{
		OnPropertyChanged(nameof(Count));
		OnPropertyChanged(IndexerName);
		RaiseCollectionChangedCore(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Remove,
			new[] { item }));
	}

	private void RaiseReset()
	{
		OnPropertyChanged(nameof(Count));
		OnPropertyChanged(IndexerName);
		RaiseCollectionChangedCore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	// Renamed to avoid Fody PropertyChanged pattern "On<PropertyName>Changed"
	protected virtual void RaiseCollectionChangedCore(NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
