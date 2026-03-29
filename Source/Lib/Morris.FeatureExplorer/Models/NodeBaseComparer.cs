using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer.Models
{
	public sealed class NodeBaseComparer : IComparer<NodeBase>
	{
		public static readonly NodeBaseComparer Instance = new NodeBaseComparer();

		private NodeBaseComparer() { }

		public int Compare(NodeBase x, NodeBase y)
		{
			if (x is null && y is null) return 0;
			if (x is null) return -1;
			if (y is null) return 1;

			bool xIsFile = x is FileNode;
			bool yIsFile = y is FileNode;

			if (xIsFile != yIsFile)
				return xIsFile ? -1 : 1;

			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}
	}
}
