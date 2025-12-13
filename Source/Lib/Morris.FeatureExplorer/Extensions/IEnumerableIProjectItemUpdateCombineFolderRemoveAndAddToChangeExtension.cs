#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class IEnumerableIProjectItemUpdateCombineFolderRemoveAndAddToChangeExtension
{
	public static IEnumerable<IProjectItemUpdate<IEntityWithId>> CombineRemoveAndAddToChange(this IEnumerable<IProjectItemUpdate<IEntityWithId>> source)
	{
		IProjectItemUpdate<IFolderSnapshot>[] folders =
			source
			.OfType<IProjectItemUpdate<IFolderSnapshot>>()
			.ToArray();

		IProjectItemUpdate<IFolderSnapshot>[] folderRemovals =
			folders
			.Where(x => x.UpdateType == UpdateType.Removed)
			.OrderBy(x => x.PreviousId.GetFolderPath())
			.ToArray();

		IProjectItemUpdate<IFolderSnapshot>[] folderAdditions =
			folders
			.Where(x => x.UpdateType == UpdateType.Added)
			.OrderBy(x => x.Current.Id.GetFolderPath())
			.ToArray();

		if (folderRemovals.Length != folderAdditions.Length || folderRemovals.Length == 0)
			return source;

		var folderChanges = new List<IProjectItemUpdate<IEntityWithId>>(folderRemovals.Length);
		IEntityRuntimeModel entityRuntime = ((FolderSnapshot)folderAdditions[0].Current).EntityRuntime;

		for (int i = 0; i < folderRemovals.Length; i++)
		{
			IProjectItemUpdate<IFolderSnapshot> removal = folderRemovals[i];
			IProjectItemUpdate<IFolderSnapshot> addition = folderAdditions[i];
			string x = addition.Current.GetType().Name;

			var update =
				new ProjectItemUpdate<IFolderSnapshot>(
					projectGuid: addition.ProjectGuid,
					previousId: removal.PreviousId,
					currentEntityState: addition.Current,
					updateType: UpdateType.Updated
				);

			folderChanges.Add(update);
		}

		IEnumerable<IProjectItemUpdate<IEntityWithId>> nonFolders = source.Except(folders);
		IEnumerable<IProjectItemUpdate<IEntityWithId>> result = nonFolders.Concat(folderChanges);
		return result;
	}
}