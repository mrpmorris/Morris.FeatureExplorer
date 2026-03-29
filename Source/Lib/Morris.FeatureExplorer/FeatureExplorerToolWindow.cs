using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace Morris.FeatureExplorer;

[Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d")]
public class FeatureExplorerToolWindow : ToolWindowPane
{
	public FeatureExplorerToolWindow() : base(null)
	{
		Caption = "Feature Explorer";
		Content = new FeatureExplorerToolWindowControl();
	}
}
