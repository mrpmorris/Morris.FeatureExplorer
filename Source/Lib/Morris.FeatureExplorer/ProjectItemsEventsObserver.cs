#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer;

internal class ProjectItemsEventsObserver<TEntity> :
	IDisposable,
	IObserver<IQueryTrackUpdates<TEntity>>
	where TEntity : IEntityWithId<TEntity>
{
	private readonly Guid ProjectGuid;
	private readonly IDisposable Subscription;
	private readonly Action<IEnumerable<IProjectItemUpdate<TEntity>>> OnBatchReceived;

	public static async ValueTask<IDisposable> CreateAsync(
		Guid projectGuid,
		IAsyncQueryable<TEntity> itemsQuery,
		Action<IEnumerable<IProjectItemUpdate<TEntity>>> onBatchReceived)
	{
		if (itemsQuery is null)
			throw new ArgumentNullException(nameof(itemsQuery));

		var instance = new ProjectItemsEventsObserver<TEntity>(projectGuid, null, onBatchReceived);
		IDisposable subscription = await itemsQuery.TrackUpdatesAsync(instance, CancellationToken.None);
		return new ProjectItemsEventsObserver<TEntity>(projectGuid, subscription, onBatchReceived);
	}

	private ProjectItemsEventsObserver(
		Guid projectGuid,
		IDisposable subscription,
		Action<IEnumerable<IProjectItemUpdate<TEntity>>> onBatchReceived)
	{
		ProjectGuid = projectGuid;
		Subscription = subscription;
		OnBatchReceived = onBatchReceived ?? throw new ArgumentNullException(nameof(onBatchReceived));
	}

	void IObserver<IQueryTrackUpdates<TEntity>>.OnNext(IQueryTrackUpdates<TEntity> value)
	{
		if (value.Updates.Count == 0)
			return;

		IEnumerable<IProjectItemUpdate<TEntity>> updates = value.Updates.ToProjectItemUpdates(ProjectGuid);
		OnBatchReceived(updates);
	}

	void IObserver<IQueryTrackUpdates<TEntity>>.OnCompleted() { }
	void IObserver<IQueryTrackUpdates<TEntity>>.OnError(Exception error) { }

	void IDisposable.Dispose()
	{
		Subscription?.Dispose();
	}
}
