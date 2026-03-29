namespace Morris.FeatureExplorer.Models;

internal class FolderNode : NodeBase
{
	public SortedObservableCollection<NodeBase> ChildNodes { get; }

	public FolderNode(string name, string relativePath)
		: base(name, relativePath)
	{
		ChildNodes = new SortedObservableCollection<NodeBase>(new NodeBaseComparer());
	}
}
