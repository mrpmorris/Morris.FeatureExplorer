using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetSourceItemNameExtension
{
	public static string GetSourceItemName(this EntityIdentity instance) =>
		instance["SourceItemName"];
}
