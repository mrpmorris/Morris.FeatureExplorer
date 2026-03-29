#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIFileSnapshotDefaultPropertiesExtension
{
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
