#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIFolderSnapshotDefaultPropertiesExtension
{
	public static IAsyncQueryable<IFolderSnapshot> WithDefaultProperties(this IAsyncQueryable<IFolderSnapshot> query)
	{
		return query.With(x => new
		{
			x.Name,
			x.RelativePath
		});
	}
}
