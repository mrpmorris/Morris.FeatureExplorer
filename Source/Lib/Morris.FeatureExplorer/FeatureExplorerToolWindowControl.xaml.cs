using System.Windows.Controls;

namespace Morris.FeatureExplorer;

public partial class FeatureExplorerToolWindowControl : UserControl
{
	public FeatureExplorerToolWindowControl()
	{
		InitializeComponent();
		DataContext = FeatureExplorerViewModel.Instance;
	}
}
