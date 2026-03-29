using System;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

internal class ProjectInfo
{
	public Guid ProjectGuid { get; }
	public string Name { get; }
	public string ProjectPath { get; }

	public ProjectInfo(Guid projectGuid, string name, string projectPath)
	{
		ProjectGuid = projectGuid;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
	}
}
