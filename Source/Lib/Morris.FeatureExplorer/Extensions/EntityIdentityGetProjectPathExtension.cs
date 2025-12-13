using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Morris.FeatureExplorer.Extensions;

internal static class EntityIdentityGetProjectPathExtension
{
	public static string GetProjectPath(this EntityIdentity instance) =>
		instance["ProjectPath"];
}
