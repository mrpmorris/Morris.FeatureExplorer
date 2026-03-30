using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Models;

namespace Morris.FeatureExplorer
{
	public partial class FeatureExplorerToolWindowControl : UserControl
	{
		public FeatureExplorerToolWindowControl()
		{
			InitializeComponent();
			DataContext = FeatureExplorerPackage.ViewModel;
			if (DataContext == null)
				Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;
			if (DataContext == null)
				DataContext = FeatureExplorerPackage.ViewModel;
		}

		private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (e.NewValue is FileNode fileNode)
				FeatureExplorerPackage.ViewModel?.SelectInSolutionExplorer(fileNode);
		}
	}
}
