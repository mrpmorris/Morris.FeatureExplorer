using System.Collections.Specialized;
using System.IO;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

public partial class FolderNode : NodeBase
{
	public SortedObservableCollection<NodeBase> ChildNodes { get; }

	public FolderNode(
		string name,
		string relativePath)
		: base(
			name: name,
			relativePath: relativePath)
	{
		ChildNodes = new SortedObservableCollection<NodeBase>(new NodeBaseComparer());
	}

	protected override string InternalGetParentPath(string fullPath) =>
		Path.GetDirectoryName(fullPath.TrimEnd(Path.DirectorySeparatorChar)) + Path.DirectorySeparatorChar;
}
