#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using System;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal interface IProjectItemUpdate<out TEntity>
	where TEntity : IEntityWithId
{
	Guid ProjectGuid { get; }
	EntityIdentity PreviousId { get; }
	TEntity Current { get; }
	UpdateType UpdateType { get; }
}

internal class ProjectItemUpdate<TEntity> : ItemUpdate<TEntity>, IProjectItemUpdate<TEntity>
	where TEntity : IEntityWithId
{
	public Guid ProjectGuid { get; }

	public ProjectItemUpdate(
		Guid projectGuid,
		EntityIdentity previousId,
		TEntity currentEntityState,
		UpdateType updateType) 
		: base(previousId, currentEntityState, updateType)
	{
		ProjectGuid = projectGuid;
	}

	public static IProjectItemUpdate<TEntity> Create(Guid projectGuid, ItemUpdate<TEntity> update) =>
		new ProjectItemUpdate<TEntity>(
			projectGuid,
			previousId: update.PreviousId!,
			currentEntityState: update.Current!,
			updateType: update.UpdateType
		);

}
