using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System.Windows.Controls;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

public partial class FeatureExplorerToolWindowControl : UserControl
{
	public FeatureExplorerToolWindowControl()
	{
		InitializeComponent();

		DataContext = FeatureExplorerViewModel.Instance;
	}
}
