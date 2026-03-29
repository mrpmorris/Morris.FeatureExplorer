#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer;

internal class SolutionProjectEventsObserver : IObserver<IQueryTrackUpdates<IProjectSnapshot>>
{
	private readonly AsyncPackage Package;
	private readonly Dictionary<EntityIdentity, CompositeDisposable> ProjectSubscriptions = new();
	private readonly Subject<IProjectItemUpdate<IEntityWithId>> UpdatesSubject = new();

	public IObservable<IList<IProjectItemUpdate<IEntityWithId>>> BatchedUpdates { get; }

	public static async ValueTask<SolutionProjectEventsObserver> CreateAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

		var instance = new SolutionProjectEventsObserver(package);
		await instance.SubscribeToSolutionAsync();
		return instance;
	}

	private SolutionProjectEventsObserver(AsyncPackage package)
	{
		Package = package ?? throw new ArgumentNullException(nameof(package));

		BatchedUpdates = UpdatesSubject
			.Synchronize()
			.ObserveOn(DispatcherScheduler.Current)
			.Buffer(() => UpdatesSubject.Throttle(TimeSpan.FromMilliseconds(1_000)))
			.Where(batch => batch.Count > 0)
			.ObserveOn(DispatcherScheduler.Current);
	}

	private async Task SubscribeToSolutionAsync()
	{
		var queryService = await Package.GetServiceAsync<IProjectSystemQueryService>();
		IAsyncQueryable<IProjectSnapshot> projectsQuery =
			queryService
			.QueryableSpace
			.Projects
			.WithDefaultProperties();

		_ = await projectsQuery.TrackUpdatesAsync(this, CancellationToken.None);
	}

	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnNext(IQueryTrackUpdates<IProjectSnapshot> value)
	{
		if (value.Updates.Count == 0)
			return;

		ThreadHelper.JoinableTaskFactory.Run(async () =>
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			foreach (ItemUpdate<IProjectSnapshot> update in value.Updates)
			{
				switch (update.UpdateType)
				{
					case UpdateType.Added:
						await HandleProjectAddedAsync(update);
						break;
					case UpdateType.Removed:
						HandleProjectRemoved(update);
						break;
				}

				UpdatesSubject.OnNext(
					ProjectItemUpdate<IEntityWithId>.Create(update.Current?.Guid ?? Guid.Empty,
						new ItemUpdate<IEntityWithId>(update.PreviousId, update.Current, update.UpdateType)));
			}
		});
	}

	private async ValueTask HandleProjectAddedAsync(ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var queryService = await Package.GetServiceAsync<IProjectSystemQueryService>();
		Guid projectGuid = update.Current.Guid;
		var subscriptions = new CompositeDisposable();

		IAsyncQueryable<IFolderSnapshot> foldersQuery =
			queryService
			.QueryableSpace
			.Projects
			.Where(p => p.Guid == projectGuid)
			.Get(p => p.Folders)
			.WithDefaultProperties();

		IDisposable folderSub = await ProjectItemsEventsObserver<IFolderSnapshot>.CreateAsync(
			projectGuid,
			foldersQuery,
			batch => { foreach (var item in batch) UpdatesSubject.OnNext(Coerce(item)); });
		subscriptions.Add(folderSub);

		IAsyncQueryable<IFileSnapshot> filesQuery =
			queryService
			.QueryableSpace
			.Projects
			.Where(p => p.Guid == projectGuid)
			.Get(p => p.Files)
			.WithDefaultProperties();

		IDisposable fileSub = await ProjectItemsEventsObserver<IFileSnapshot>.CreateAsync(
			projectGuid,
			filesQuery,
			batch => { foreach (var item in batch) UpdatesSubject.OnNext(Coerce(item)); });
		subscriptions.Add(fileSub);

		ProjectSubscriptions[update.Current.Id] = subscriptions;
	}

	private void HandleProjectRemoved(ItemUpdate<IProjectSnapshot> update)
	{
		if (update.PreviousId is not null && ProjectSubscriptions.TryGetValue(update.PreviousId, out var subscriptions))
		{
			ProjectSubscriptions.Remove(update.PreviousId);
			subscriptions.Dispose();
		}
	}

	private static IProjectItemUpdate<IEntityWithId> Coerce<TEntity>(IProjectItemUpdate<TEntity> update)
		where TEntity : IEntityWithId
	{
		return new ProjectItemUpdate<IEntityWithId>(
			update.ProjectGuid,
			update.PreviousId,
			update.Current,
			update.UpdateType);
	}

	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnCompleted() { }
	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnError(Exception error) { }
}
