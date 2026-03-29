#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class IEnumerableIProjectItemUpdateCombineFolderRemoveAndAddToChangeExtension
{
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
}
