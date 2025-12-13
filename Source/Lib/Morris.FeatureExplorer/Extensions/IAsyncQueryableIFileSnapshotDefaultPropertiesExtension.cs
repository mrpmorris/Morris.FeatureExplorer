using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class IAsyncQueryableIFileSnapshotDefaultPropertiesExtension
{
	public static IAsyncQueryable<IFileSnapshot> WithDefaultProperties(this IAsyncQueryable<IFileSnapshot> source) =>
		source
		.With(x =>
			new
			{
				x.Id,
				x.Extension,
				x.FileName,
				x.ItemName,
				x.ItemType,
				x.LinkPath,
				x.Path,
				x.SharedPath,
				x.VisualPath,
				x.IsSearchable,
				x.IsHidden
			}
		);
}
