#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetProjectPathExtension
{
	public static string GetProjectPath(this EntityIdentity identity)
	{
		identity.TryGetValue("Path", out string path);
		return path;
	}
}
