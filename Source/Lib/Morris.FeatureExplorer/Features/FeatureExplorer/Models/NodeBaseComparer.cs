using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

public partial class NodeBaseComparer : IComparer<NodeBase>
{
	public int Compare(NodeBase first, NodeBase second)
	{
		if (first is null && second is null) return 0;
		if (first is null) return 1;
		if (second is null) return -1;

		// Folders come before files
		bool firstIsFolder = first is FolderNode;
		bool secondIsFolder = second is FolderNode;

		if (firstIsFolder && !secondIsFolder) return 1;
		if (!firstIsFolder && secondIsFolder) return -1;

		// Both are same type, sort alphabetically by name
		return string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
	}
}

