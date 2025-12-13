using EnvDTE;
using PropertyChanged;
using System;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

[AddINotifyPropertyChangedInterface]
public abstract partial class NodeBase
{
	public bool IsExpanded { get; set; }
	public string Name { get; set; }
	public string ParentPath { get; }
	public ObservableSet<Guid> Projects { get; } = new();
	public string RelativePath { get; set; }
	protected abstract string InternalGetParentPath(string fullPath);
	
	protected NodeBase(
		string name,
		string relativePath)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
		ParentPath = GetParentPath(RelativePath);
	}


	public string GetParentPath(string fullPath)
	{
		string result = InternalGetParentPath(fullPath);
		return result;
	}

}
