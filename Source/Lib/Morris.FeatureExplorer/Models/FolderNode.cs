namespace Morris.FeatureExplorer.Models
{
	public sealed class FolderNode : NodeBase
	{
		public SortedObservableCollection<NodeBase> Children { get; } =
			new SortedObservableCollection<NodeBase>(NodeBaseComparer.Instance);
		public FolderNode CreationParent { get; set; }
		public bool IsNew { get; set; }

		public FolderNode(string name) : base(name) { }
	}
}
