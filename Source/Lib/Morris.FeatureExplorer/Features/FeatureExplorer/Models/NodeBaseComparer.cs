using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

internal class NodeBaseComparer : IComparer<NodeBase>
{
	public int Compare(NodeBase first, NodeBase second)
	{
		bool firstIsFolder = first is FolderNode;
		bool secondIsFolder = second is FolderNode;

		if (firstIsFolder && !secondIsFolder)
			return -1;
		if (!firstIsFolder && secondIsFolder)
			return 1;

		return string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
	}
}
