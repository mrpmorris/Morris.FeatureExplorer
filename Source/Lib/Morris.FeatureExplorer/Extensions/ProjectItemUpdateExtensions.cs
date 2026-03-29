#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class ProjectItemUpdateExtensions
{
	public static IEnumerable<IProjectItemUpdate<TEntity>> ToProjectItemUpdates<TEntity>(
		this IEnumerable<ItemUpdate<TEntity>> source,
		Guid projectGuid)
		where TEntity : IEntityWithId<TEntity>
	=>
		source
		.Select(x => ProjectItemUpdate<TEntity>.Create(projectGuid, x))
		.ToArray();

	public static IEnumerable<IProjectItemUpdate<IEntityWithId>> CombineRemoveAndAddToChange(
		this IEnumerable<IProjectItemUpdate<IEntityWithId>> source)
	{
		var updates = source.ToList();
		var removals = updates
			.Where(x => x.UpdateType is UpdateType.Removed && x.Current is IFolderSnapshot)
			.ToList();

		var additions = updates
			.Where(x => x.UpdateType is UpdateType.Added && x.Current is IFolderSnapshot)
			.ToList();

		var result = new List<IProjectItemUpdate<IEntityWithId>>(updates);

		foreach (var removal in removals)
		{
			string removedPath = removal.PreviousId?.GetFolderPath();
			if (removedPath is null)
				continue;

			var matchingAddition = additions.FirstOrDefault(a =>
			{
				var folder = a.Current as IFolderSnapshot;
				return folder?.RelativePath == removedPath;
			});

			if (matchingAddition is not null)
			{
				result.Remove(removal);
				result.Remove(matchingAddition);
				result.Add(new ProjectItemUpdate<IEntityWithId>(
					matchingAddition.ProjectGuid,
					removal.PreviousId,
					matchingAddition.Current,
					UpdateType.Updated));
				additions.Remove(matchingAddition);
			}
		}

		return result;
	}

	public static IEnumerable<IProjectItemUpdate<IEntityWithId>> SortByProcessingOrder(
		this IEnumerable<IProjectItemUpdate<IEntityWithId>> source)
	{
		return source.OrderBy(x => GetUpdateTypePriority(x.UpdateType))
			.ThenBy(x => GetEntityTypePriority(x.Current));
	}

	public static string GetRelativePath(this IProjectItemUpdate<IEntityWithId> update)
	{
		return update.Current switch
		{
			IFolderSnapshot folder => folder.RelativePath,
			IFileSnapshot file => file.VisualPath ?? file.Path,
			IProjectSnapshot project => project.Path,
			_ => null
		};
	}

	public static string GetName(this IProjectItemUpdate<IEntityWithId> update)
	{
		return update.Current switch
		{
			IFolderSnapshot folder => folder.Name,
			IFileSnapshot file => file.FileName,
			IProjectSnapshot project => project.Name,
			_ => null
		};
	}

	private static int GetUpdateTypePriority(UpdateType updateType) =>
		updateType switch
		{
			UpdateType.Removed => 0,
			UpdateType.Updated => 1,
			UpdateType.Added => 2,
			_ => 3,
		};

	private static int GetEntityTypePriority(IEntityWithId entity) =>
		entity switch
		{
			IProjectSnapshot => 0,
			IFolderSnapshot => 1,
			IFileSnapshot => 2,
			_ => 3,
		};
}
