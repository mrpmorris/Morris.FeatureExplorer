#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Morris.FeatureExplorer.Features.FeatureExplorer;

namespace Morris.FeatureExplorer.Extensions;

internal static class IProjectItemUpdateGetFullPathExtension
{
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
}
