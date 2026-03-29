#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Morris.FeatureExplorer.Extensions;

internal static class IEnumerableItemUpdateToProjectItemUpdatesExtension
{
	public static IEnumerable<IProjectItemUpdate<TEntity>> ToProjectItemUpdates<TEntity>(
		this IEnumerable<ItemUpdate<TEntity>> source,
		Guid projectGuid)
		where TEntity : IEntityWithId<TEntity>
	=>
		source
		.Select(x => ProjectItemUpdate<TEntity>.Create(projectGuid, x))
		.ToArray();
}
