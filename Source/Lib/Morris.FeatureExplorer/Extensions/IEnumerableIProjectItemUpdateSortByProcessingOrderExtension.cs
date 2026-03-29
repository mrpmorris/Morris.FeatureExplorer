#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class IEnumerableIProjectItemUpdateSortByProcessingOrderExtension
{
	public static IEnumerable<IProjectItemUpdate<IEntityWithId>> SortByProcessingOrder(
		this IEnumerable<IProjectItemUpdate<IEntityWithId>> source)
	{
		return source.OrderBy(x => GetUpdateTypePriority(x.UpdateType))
			.ThenBy(x => GetEntityTypePriority(x.Current));
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
