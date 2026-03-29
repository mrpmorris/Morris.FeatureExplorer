#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetFolderPathExtension
{
	public static string GetFolderPath(this EntityIdentity identity)
	{
		identity.TryGetValue("RelativePath", out string path);
		return path;
	}
}
