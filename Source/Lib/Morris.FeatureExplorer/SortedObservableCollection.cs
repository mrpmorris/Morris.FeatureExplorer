using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Morris.FeatureExplorer;

public class SortedObservableCollection<T> : ObservableCollection<T>
{
	private readonly IComparer<T> Comparer;

	public SortedObservableCollection(IComparer<T> comparer)
	{
		Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
	}

	protected override void InsertItem(int index, T item)
	{
		// Find the correct sorted position
		int sortedIndex = FindInsertIndex(item);
		base.InsertItem(sortedIndex, item);
	}

	protected override void SetItem(int index, T item)
	{
		// Remove and re-insert to maintain sort order
		RemoveAt(index);
		InsertItem(0, item); // InsertItem will find correct position
	}

	private int FindInsertIndex(T item)
	{
		int left = 0;
		int right = Count;

		while (left < right)
		{
			int mid = left + (right - left) / 2;
			if (Comparer.Compare(item, this[mid]) < 0)
				right = mid;
			else
				left = mid + 1;
		}

		return left;
	}

	public new void Add(T item)
	{
		// InsertItem will find correct position
		InsertItem(0, item); 
	}

	public void AddRange(IEnumerable<T> items)
	{
		// InsertItem will find correct position
		foreach (var item in items)
			InsertItem(0, item);
	}
}