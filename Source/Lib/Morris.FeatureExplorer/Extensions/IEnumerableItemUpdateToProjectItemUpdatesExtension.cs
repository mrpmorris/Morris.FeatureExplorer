#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
		Guid projectId)
		where TEntity : IEntityWithId<TEntity>
	=>
		source
		.Select(x => ProjectItemUpdate<TEntity>.Create(projectId, x))
		.ToArray();
}

