using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Morris.FeatureExplorer;

internal class CompositeDisposable : IDisposable
{
	private bool Disposed;
	private ImmutableArray<IDisposable> Disposables;

	public CompositeDisposable(params IEnumerable<IDisposable> disposables)
	{
		if (disposables is null)
			throw new ArgumentNullException(nameof(disposables));
		Disposables = disposables.ToImmutableArray();
		if (Disposables.Length == 0)
			throw new ArgumentException(
				paramName: nameof(disposables),
				message: "Cannot be empty");
	}

	void IDisposable.Dispose()
	{
		if (Disposed) return;
		Disposed = true;
		foreach (IDisposable disposable in Disposables)
			disposable.Dispose();
	}
}
