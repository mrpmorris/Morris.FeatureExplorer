#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal class SolutionEventsObserver : IObserver<IQueryTrackUpdates<IProjectSnapshot>>
{
	public IObservable<IProjectItemUpdate<IEntityWithId>> Updates => UpdatesSubject;

	private readonly AsyncPackage Package;
	private readonly Dictionary<EntityIdentity, IDisposable> ProjectEventsSubscriptions = new();
	private readonly ISubject<IProjectItemUpdate<IEntityWithId>> UpdatesSubject;

	public static async ValueTask<IObservable<IProjectItemUpdate<IEntityWithId>>> CreateAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

		var instance = new SolutionEventsObserver(package);
		await instance.InitializeAsync();
		return instance.Updates;
	}

	private SolutionEventsObserver(AsyncPackage package)
	{
		Package = package ?? throw new ArgumentNullException(nameof(package));

		UpdatesSubject = new Subject<IProjectItemUpdate<IEntityWithId>>();
	}

	private async Task InitializeAsync()
	{
		var queryService = await Package.GetServiceAsync<IProjectSystemQueryService>();
		IAsyncQueryable<IProjectSnapshot> projectsQuery =
			queryService
			.QueryableSpace
			.Projects
			.With(x =>
				new
				{
					x.Id,
					x.Guid,
					x.Name,
					x.Path,
					x.VisualPath
				});

		_ = await projectsQuery.TrackUpdatesAsync(this, CancellationToken.None);
	}

	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnCompleted() { }
	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnError(Exception error) { }

	void IObserver<IQueryTrackUpdates<IProjectSnapshot>>.OnNext(IQueryTrackUpdates<IProjectSnapshot> value)
	{
		if (value.Updates.Count == 0)
			return;

		ThreadHelper.JoinableTaskFactory.Run(
			async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				foreach (ItemUpdate<IProjectSnapshot> update in value.Updates)
				{
					if (update.UpdateType is UpdateType.Added)
					{
						await HandleProjectAddedAsync(update);
					}
					else if (update.UpdateType is UpdateType.Removed)
					{
						await HandleProjectRemovedAsync(update);
					}
				}
			}
		);
	}

	private async ValueTask HandleProjectAddedAsync(ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		var projectViewModel = await ProjectEventsSubscriber<IFolderSnapshot>.CreateAsync(Package, update.Current.Guid);
		ProjectEventsSubscriptions.Add(update.Current.Id, projectViewModel);
	}

	private async ValueTask HandleProjectRemovedAsync(ItemUpdate<IProjectSnapshot> update)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (ProjectEventsSubscriptions.TryGetValue(update.PreviousId!, out ProjectViewModel projectViewModel))
		{
			ProjectEventsSubscriptions.Remove(update.PreviousId!);
			((IDisposable)projectViewModel).Dispose();
		}
	}
}
