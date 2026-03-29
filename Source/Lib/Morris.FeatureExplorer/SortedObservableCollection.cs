using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Morris.FeatureExplorer;

internal class SortedObservableCollection<T> : ObservableCollection<T>
{
	private readonly IComparer<T> Comparer;

	public SortedObservableCollection(IComparer<T> comparer)
	{
		Comparer = comparer ?? throw new System.ArgumentNullException(nameof(comparer));
	}

	public void AddSorted(T item)
	{
		int index = FindInsertionIndex(item);
		InsertItem(index, item);
	}

	private int FindInsertionIndex(T item)
	{
		int low = 0;
		int high = Count - 1;
		while (low <= high)
		{
			int mid = (low + high) / 2;
			int comparison = Comparer.Compare(this[mid], item);
			if (comparison < 0)
				low = mid + 1;
			else
				high = mid - 1;
		}
		return low;
	}
}
