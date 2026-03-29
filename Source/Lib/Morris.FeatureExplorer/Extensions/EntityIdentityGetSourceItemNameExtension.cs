#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetSourceItemNameExtension
{
	public static string GetSourceItemName(this EntityIdentity identity)
	{
		identity.TryGetValue("FileName", out string name);
		return name;
	}
}
