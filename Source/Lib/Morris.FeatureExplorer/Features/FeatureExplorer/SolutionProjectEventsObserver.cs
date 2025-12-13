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

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal class SolutionProjectEventsObserver :
	IDisposable,
	IObserver<IQueryTrackUpdates<IProjectSnapshot>>
{
	public IObservable<IEnumerable<IProjectItemUpdate<IEntityWithId>>> Observable {  get; }

	private readonly AsyncPackage Package;
	private IDisposable SolutionProjectsSubscription;
	private readonly Dictionary<EntityIdentity, IDisposable> ProjectItemsSubscriptions = new();
	private readonly ISubject<IProjectItemUpdate<IEntityWithId>> Subject;

	public static async ValueTask<SolutionProjectEventsObserver> CreateAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

		var instance = new SolutionProjectEventsObserver(package);
		await instance.InitializeAsync();
		return instance;
	}

	void IDisposable.Dispose()
	{
		SolutionProjectsSubscription.Dispose();
	}

	private SolutionProjectEventsObserver(AsyncPackage package)
	{
		Package = package ?? throw new ArgumentNullException(nameof(package));
		Subject = new Subject<IProjectItemUpdate<IEntityWithId>>();
		Observable =
			Subject
			.Synchronize()
			.ObserveOn(DispatcherScheduler.Current)
			.Buffer(() => Subject.Throttle(TimeSpan.FromMilliseconds(1_000)))
			.Where(batch => batch.Count > 0)
			.ObserveOn(DispatcherScheduler.Current)
			.Select(batch => batch.CombineRemoveAndAddToChange())
			.Select(batch => batch.SortByProcessingOrder());
	}

	private async Task InitializeAsync()
	{
		var queryService = await Package.GetServiceAsync<IProjectSystemQueryService>();
		IAsyncQueryable<IProjectSnapshot> projectsQuery =
			queryService
			.QueryableSpace
			.Projects
			.WithDefaultProperties();

		SolutionProjectsSubscription = await projectsQuery.TrackUpdatesAsync(this, CancellationToken.None);
	}


	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnNext(IQueryTrackUpdates<IProjectSnapshot> value)
	{
		if (value.Updates.Count == 0)
			return;

		ThreadHelper.JoinableTaskFactory.Run(async () =>
		{
			var queryService = await Package.GetServiceAsync<IProjectSystemQueryService>();
			IProjectModelQueryableSpace querySpace = await queryService.GetProjectModelQueryableSpaceAsync();

			foreach (ItemUpdate<IProjectSnapshot> update in value.Updates)
			{
				switch (update.UpdateType)
				{
					case UpdateType.Added:
						await HandleProjectAddedAsync(querySpace, update);
						break;

					case UpdateType.Removed:
						await HandleProjectRemovedAsync(update);
						break;

					case UpdateType.Updated:
						await HandleProjectUpdatedAsync(update);
						break;

					default:
						throw new NotImplementedException(update.UpdateType.ToString());
				}
			}
		});
	}

	private async ValueTask HandleProjectAddedAsync(
		IProjectModelQueryableSpace querySpace,
		ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		IAsyncQueryable<IProjectSnapshot> projectQuery =
			querySpace
			.ProjectsByProjectGuid(update.Current!.Guid)
			.With(x => x.Guid);

		IAsyncEnumerable<IQueryResultItem<IProjectSnapshot>> projects = projectQuery.QueryAsync(CancellationToken.None);
		await foreach (IQueryResultItem<IProjectSnapshot> project in projects)
		{
			IAsyncQueryable<IProjectSnapshot> projectQueryable = project.Value.AsQueryable();

			IDisposable projectSubscription = await ProjectItemsEventsObserver<IProjectSnapshot>
				.CreateAsync(
					projectGuid: project.Value.Guid,
					itemsQuery: projectQuery.WithDefaultProperties(),
					onProjectItemUpdate: NotifyProjectUpdatedAsync
				);

			IDisposable foldersSubscription = await ProjectItemsEventsObserver<IFolderSnapshot>
				.CreateAsync(
					projectGuid: project.Value.Guid,
					itemsQuery: projectQueryable.With(x => x.Folders).Get(x => x.Folders).WithDefaultProperties(),
					onProjectItemUpdate: NotifyFolderUpdatedAsync
				);

			IDisposable filesSubscription = await ProjectItemsEventsObserver<IFileSnapshot>
				.CreateAsync(
					projectGuid: project.Value.Guid,
					itemsQuery: projectQueryable.With(x => x.Files).Get(x => x.Files).WithDefaultProperties(),
					onProjectItemUpdate: NotifyFileUpdatedAsync
				);

			IDisposable projectSubscriptions =
				new CompositeDisposable(
					projectSubscription,
					foldersSubscription,
					filesSubscription
				);

			ProjectItemsSubscriptions.Add(update.Current.Id, projectSubscriptions);
		}
	}

	private async ValueTask HandleProjectUpdatedAsync(ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
	}

	private async ValueTask HandleProjectRemovedAsync(ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (ProjectItemsSubscriptions.TryGetValue(update.PreviousId!, out IDisposable subscription))
		{
			ProjectItemsSubscriptions.Remove(update.PreviousId!);
			subscription.Dispose();
		}
	}

	private async ValueTask NotifyProjectUpdatedAsync(IProjectItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		Subject.OnNext(update);
	}

	private async ValueTask NotifyFolderUpdatedAsync(IProjectItemUpdate<IFolderSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		Subject.OnNext(update);
	}

	private async ValueTask NotifyFileUpdatedAsync(IProjectItemUpdate<IFileSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		Subject.OnNext(update);
	}

	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnCompleted() { }
	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnError(Exception error) { }
}
