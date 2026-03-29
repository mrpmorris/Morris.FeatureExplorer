#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class QueryableDefaultPropertiesExtensions
{
	public static IAsyncQueryable<IProjectSnapshot> WithDefaultProperties(this IAsyncQueryable<IProjectSnapshot> query)
	{
		return query.With(x => new
		{
			x.Id,
			x.Guid,
			x.Name,
			x.Path,
			x.VisualPath
		});
	}

	public static IAsyncQueryable<IFolderSnapshot> WithDefaultProperties(this IAsyncQueryable<IFolderSnapshot> query)
	{
		return query.With(x => new
		{
			x.Name,
			x.RelativePath
		});
	}

	public static IAsyncQueryable<IFileSnapshot> WithDefaultProperties(this IAsyncQueryable<IFileSnapshot> query)
	{
		return query.With(x => new
		{
			x.FileName,
			x.Path,
			x.ItemName,
			x.ItemType,
			x.VisualPath,
			x.Extension
		});
	}
}
