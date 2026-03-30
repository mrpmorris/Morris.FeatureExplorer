using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Morris.FeatureExplorer
{
	public class SortedObservableCollection<T> : ObservableCollection<T>
	{
		private readonly IComparer<T> Comparer;

		public SortedObservableCollection(IComparer<T> comparer)
		{
			Comparer = comparer;
		}

		public void AddSorted(T item)
		{
			int index = FindInsertionIndex(item);
			InsertItem(index, item);
		}

		public void Reposition(T item)
		{
			int index = IndexOf(item);
			if (index >= 0)
			{
				RemoveAt(index);
				AddSorted(item);
			}
		}

		private int FindInsertionIndex(T item)
		{
			int low = 0;
			int high = Count - 1;

			while (low <= high)
			{
				int mid = low + (high - low) / 2;
				int comparison = Comparer.Compare(Items[mid], item);

				if (comparison <= 0)
					low = mid + 1;
				else
					high = mid - 1;
			}

			return low;
		}
	}
}
