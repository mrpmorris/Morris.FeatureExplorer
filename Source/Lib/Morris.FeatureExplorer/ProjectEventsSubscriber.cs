#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal class ProjectEventsSubscriber<TEntity> :
	IDisposable,
	IObserver<IQueryTrackUpdates<TEntity>>
	where TEntity : IEntityWithId<TEntity>
{
	private readonly Guid ProjectGuid;
	private IDisposable ItemsSubscription { get; }
	private Func<IProjectItemUpdate<TEntity>, ValueTask> OnProjectItemUpdate;

	public static async ValueTask<IDisposable> CreateAsync(
		Guid projectGuid,
		IAsyncQueryable<TEntity> itemsQuery,
		Func<IProjectItemUpdate<TEntity>, ValueTask> onProjectItemUpdate)
	{
		if (itemsQuery is null)
			throw new ArgumentNullException(nameof(itemsQuery));

		var instance = new ProjectEventsSubscriber<TEntity>(projectGuid, onProjectItemUpdate);
		IDisposable itemsSubscription = await itemsQuery.TrackUpdatesAsync(instance, CancellationToken.None);
		return instance;
	}

	void IDisposable.Dispose()
	{
		ItemsSubscription.Dispose();
	}

	private ProjectEventsSubscriber(
		Guid projectGuid,
		Func<IProjectItemUpdate<TEntity>, ValueTask> onProjectItemUpdate)
	{
		ProjectGuid = projectGuid;
		OnProjectItemUpdate = onProjectItemUpdate ?? throw new ArgumentNullException(nameof(onProjectItemUpdate));
	}

	void IObserver<IQueryTrackUpdates<TEntity>>.OnNext(IQueryTrackUpdates<TEntity> value)
	{
		if (value.Updates.Count == 0)
			return;

		IEnumerable<IProjectItemUpdate<TEntity>> orderedUpdates = value.Updates.ToProjectItemUpdates(ProjectGuid);
		ThreadHelper.JoinableTaskFactory.Run(async () =>
		{
			foreach (IProjectItemUpdate<TEntity> update in orderedUpdates)
			{
				await OnProjectItemUpdate(update);
			}
		});
	}

	void IObserver<IQueryTrackUpdates<TEntity>>.OnCompleted() { }
	void IObserver<IQueryTrackUpdates<TEntity>>.OnError(Exception error) { }

}