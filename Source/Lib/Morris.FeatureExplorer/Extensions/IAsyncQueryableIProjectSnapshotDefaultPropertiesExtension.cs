using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIProjectSnapshotDefaultPropertiesExtension
{
	public static IAsyncQueryable<IProjectSnapshot> WithDefaultProperties(this IAsyncQueryable<IProjectSnapshot> source) =>
		source
		.With(x =>
			new
			{
				x.Id,
				x.Guid,
				x.Name,
				x.Path,
				x.VisualPath
			}
		);
}
