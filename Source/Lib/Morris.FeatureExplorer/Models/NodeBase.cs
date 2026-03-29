using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Morris.FeatureExplorer.Models;

[AddINotifyPropertyChangedInterface]
internal abstract partial class NodeBase : INotifyPropertyChanged
{
	public string Name { get; set; }
	public string RelativePath { get; set; }
	public bool IsExpanded { get; set; }
	public HashSet<Guid> Projects { get; } = new();

	public event PropertyChangedEventHandler PropertyChanged = delegate { };

	protected NodeBase(string name, string relativePath)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
	}
}
