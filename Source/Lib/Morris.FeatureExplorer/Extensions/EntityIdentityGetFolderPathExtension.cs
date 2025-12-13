using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetFolderPathExtension
{
	public static string GetFolderPath(this EntityIdentity instance) =>
		instance["FolderPath"];
}
