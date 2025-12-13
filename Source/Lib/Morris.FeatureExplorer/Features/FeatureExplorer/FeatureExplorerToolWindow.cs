using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

[Guid("F5465841-A4D2-43A7-935A-DE2BB14C7CF6")]
public sealed class FeatureExplorerToolWindow : ToolWindowPane
{
	public FeatureExplorerToolWindow() : base(null)
	{
		Caption = "Feature Explorer";
		Content = new FeatureExplorerToolWindowControl();
	}
}
