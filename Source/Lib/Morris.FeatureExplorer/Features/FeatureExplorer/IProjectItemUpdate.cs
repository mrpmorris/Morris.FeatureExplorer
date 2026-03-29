#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
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

internal class ProjectItemUpdate<TEntity> : IProjectItemUpdate<TEntity>
	where TEntity : IEntityWithId
{
	public Guid ProjectGuid { get; }
	public EntityIdentity PreviousId { get; }
	public TEntity Current { get; }
	public UpdateType UpdateType { get; }

	public ProjectItemUpdate(
		Guid projectGuid,
		EntityIdentity previousId,
		TEntity current,
		UpdateType updateType)
	{
		ProjectGuid = projectGuid;
		PreviousId = previousId;
		Current = current;
		UpdateType = updateType;
	}

	public static IProjectItemUpdate<TEntity> Create(Guid projectGuid, ItemUpdate<TEntity> update) =>
		new ProjectItemUpdate<TEntity>(
			projectGuid,
			previousId: update.PreviousId,
			current: update.Current,
			updateType: update.UpdateType);
}
