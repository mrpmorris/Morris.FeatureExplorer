using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Morris.FeatureExplorer
{
	[Guid("B7E3A1F0-5C2D-4E8A-9F1B-3D6C7E8F9A0B")]
	public sealed class FeatureExplorerToolWindow : ToolWindowPane
	{
		public FeatureExplorerToolWindow() : base(null)
		{
			Caption = "Feature Explorer 2";
			Content = new FeatureExplorerToolWindowControl();
		}
	}
}
