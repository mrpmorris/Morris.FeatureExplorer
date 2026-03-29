#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIProjectSnapshotDefaultPropertiesExtension
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
}
