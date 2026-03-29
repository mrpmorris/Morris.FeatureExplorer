using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer;

internal sealed class CompositeDisposable : IDisposable
{
	private readonly List<IDisposable> Disposables = new();

	public void Add(IDisposable disposable)
	{
		if (disposable is null)
			throw new ArgumentNullException(nameof(disposable));
		Disposables.Add(disposable);
	}

	public void Dispose()
	{
		foreach (IDisposable disposable in Disposables)
			disposable.Dispose();
		Disposables.Clear();
	}
}
