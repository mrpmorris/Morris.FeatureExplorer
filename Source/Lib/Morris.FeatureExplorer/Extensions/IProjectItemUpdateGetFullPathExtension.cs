using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System;

namespace Morris.FeatureExplorer.Extensions;

internal static class IProjectItemUpdateGetFullPathExtension
{
	public static string GetFullPath(this IProjectItemUpdate<IEntityWithId> item) =>
		GetPathFromEntityIdentity(item, x => x.Current.Id);

	public static string GetPreviousFullPath(this IProjectItemUpdate<IEntityWithId> item) =>
		GetPathFromEntityIdentity(item, x => x.PreviousId);

	public static string GetPathFromEntityIdentity(
		this IProjectItemUpdate<IEntityWithId> item,
		Func<IProjectItemUpdate<IEntityWithId>, EntityIdentity> getId)
	=>
		item switch {
			IProjectItemUpdate<IProjectSnapshot> project => getId(project)?.GetProjectPath(),
			IProjectItemUpdate<IFolderSnapshot> folder => getId(folder)?.GetFolderPath(),
			IProjectItemUpdate<IFileSnapshot> file => getId(file)?.GetSourceItemName(),
			_ => throw new NotImplementedException(item.GetType().FullName)
		};


}
