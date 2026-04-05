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

		private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			DependencyObject source = e.OriginalSource as DependencyObject;
			while (source != null && !(source is TreeViewItem))
				source = VisualTreeHelper.GetParent(source);

			if (!(source is TreeViewItem treeViewItem) || !(treeViewItem.DataContext is FileNode fileNode))
				return;

			if (fileNode.SourcePaths.Count == 0)
				return;

			string path = null;
			foreach (string sourcePath in fileNode.SourcePaths)
			{
				path = sourcePath;
				break;
			}

			var dte = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
			dte?.ItemOperations?.OpenFile(path);
			e.Handled = true;
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
			ToolWindow.ShowItemContextMenu(fileNode, hierarchy, itemId, screenPoint);
			e.Handled = true;
		}

		private void OnRenameTextBoxIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is TextBox textBox && textBox.Visibility == Visibility.Visible)
			{
#pragma warning disable VSTHRD001, VSTHRD110
				textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new System.Action(() =>
				{
					textBox.Focus();
					int dotIndex = textBox.Text.IndexOf('.');
					textBox.Select(0, dotIndex >= 0 ? dotIndex : textBox.Text.Length);
				}));
#pragma warning restore VSTHRD001, VSTHRD110
			}
		}

		private void OnRenameTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (sender is TextBox textBox && textBox.DataContext is FileNode fileNode)
			{
				if (e.Key == Key.Enter)
				{
					CommitRename(textBox, fileNode);
					e.Handled = true;
				}
				else if (e.Key == Key.Escape)
				{
					CancelRename(textBox, fileNode);
					e.Handled = true;
				}
			}
		}

		private void OnRenameTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is TextBox textBox && textBox.DataContext is FileNode fileNode && fileNode.IsEditing)
				CancelRename(textBox, fileNode);
		}

		private void CancelRename(TextBox textBox, FileNode fileNode)
		{
			textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
			fileNode.IsEditing = false;
		}

		private void CommitRename(TextBox textBox, FileNode fileNode)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			string newName = textBox.Text;
			string oldName = fileNode.Name;

			if (newName == oldName || string.IsNullOrWhiteSpace(newName))
			{
				CancelRename(textBox, fileNode);
				return;
			}

			fileNode.IsEditing = false;

			try
			{
				var dte = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
				string path = fileNode.SourcePaths.GetEnumerator().Current;
				foreach (string sourcePath in fileNode.SourcePaths)
				{
					path = sourcePath;
					break;
				}

				EnvDTE.ProjectItem projectItem = dte?.Solution?.FindProjectItem(path);
				if (projectItem != null)
				{
					projectItem.Name = newName;
					SelectNodeByName(MainTreeView.ItemContainerGenerator, MainTreeView.Items, newName);
					return;
				}
			}
			catch
			{
			}

			fileNode.Name = oldName;
		}

		private bool SelectNodeByName(ItemContainerGenerator generator, ItemCollection items, string name)
		{
			foreach (object item in items)
			{
				if (item is FileNode fn && fn.Name == name)
				{
					if (generator.ContainerFromItem(item) is TreeViewItem tvi)
						tvi.IsSelected = true;
					return true;
				}

				if (item is FolderNode && generator.ContainerFromItem(item) is TreeViewItem folderTvi)
				{
					folderTvi.IsExpanded = true;
					folderTvi.UpdateLayout();
					if (SelectNodeByName(folderTvi.ItemContainerGenerator, folderTvi.Items, name))
						return true;
				}
			}
			return false;
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
