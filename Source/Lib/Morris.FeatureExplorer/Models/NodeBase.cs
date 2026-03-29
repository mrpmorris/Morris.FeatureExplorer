using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer.Models
{
	public abstract class NodeBase
	{
		public string Name { get; set; }
		public HashSet<string> SourcePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		protected NodeBase(string name)
		{
			Name = name;
		}
	}
}
