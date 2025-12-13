using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIFolderSnapshotDefaultPropertiesExtension
{
	public static IAsyncQueryable<IFolderSnapshot> WithDefaultProperties(this IAsyncQueryable<IFolderSnapshot> source) =>
		source
		.With(x =>
			new
			{
				x.Id,
				x.Name,
				x.RelativePath,
				x.SharedPath,
			}
		);
}
