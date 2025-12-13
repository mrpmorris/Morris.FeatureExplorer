using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

internal class ProjectInfo
{
	public Guid ProjectGuid { get; }
	public string FeaturesFolderPath { get; }
	public string Name { get; }

	public ProjectInfo(Guid projectGuid, string featuresFolderPath, string name)
	{
		ProjectGuid = projectGuid;
		FeaturesFolderPath = featuresFolderPath ?? throw new ArgumentNullException(nameof(featuresFolderPath));
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}
}