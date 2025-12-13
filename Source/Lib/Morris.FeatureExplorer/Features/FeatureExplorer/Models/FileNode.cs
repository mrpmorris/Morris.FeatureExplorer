using System.IO;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

public class FileNode : NodeBase
{
	public FileNode(
		string name,
		string relativePath)
		: base(
			name: name,
			relativePath: relativePath)
	{
	}

	protected override string InternalGetParentPath(string fullPath) =>
		Path.GetDirectoryName(fullPath) + Path.DirectorySeparatorChar;
}