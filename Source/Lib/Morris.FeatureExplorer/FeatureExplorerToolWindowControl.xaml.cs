using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Morris.FeatureExplorer.Models;

namespace Morris.FeatureExplorer
{
	public partial class FeatureExplorerToolWindowControl : UserControl
	{
		private readonly FeatureExplorerToolWindow ToolWindow;

		public FeatureExplorerToolWindowControl(FeatureExplorerToolWindow toolWindow)
		{
			ToolWindow = toolWindow;
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
			if (e.NewValue is FileNode fileNode
				&& FeatureExplorerPackage.ViewModel != null
				&& FeatureExplorerPackage.ViewModel.TryResolveHierarchyItem(fileNode, out IVsHierarchy hierarchy, out uint itemId))
			{
				ToolWindow.NotifySelectionChanged(hierarchy, itemId);
			}
		}
	}
}
