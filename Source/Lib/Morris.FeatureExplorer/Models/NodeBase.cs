using PropertyChanged;
using System;
using System.Collections.Generic;

namespace Morris.FeatureExplorer.Models
{
	[AddINotifyPropertyChangedInterface]
	public abstract class NodeBase
	{
		public bool IsEditing { get; set; }
		public string Name { get; set; }
		public HashSet<string> SourcePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		protected NodeBase(string name)
		{
			Name = name;
		}
	}
}
