using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

		private void OnTreeViewRightClick(object sender, MouseButtonEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			DependencyObject source = e.OriginalSource as DependencyObject;
			while (source != null && !(source is TreeViewItem))
				source = VisualTreeHelper.GetParent(source);

			if (!(source is TreeViewItem treeViewItem) || !(treeViewItem.DataContext is FileNode fileNode))
				return;

			if (FeatureExplorerPackage.ViewModel == null
				|| !FeatureExplorerPackage.ViewModel.TryResolveHierarchyItem(fileNode, out IVsHierarchy hierarchy, out uint itemId))
				return;

			treeViewItem.IsSelected = true;

			var treeView = (TreeView)sender;
			Point screenPoint = treeView.PointToScreen(e.GetPosition(treeView));
			ToolWindow.ShowItemContextMenu(hierarchy, itemId, screenPoint);
			e.Handled = true;
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
