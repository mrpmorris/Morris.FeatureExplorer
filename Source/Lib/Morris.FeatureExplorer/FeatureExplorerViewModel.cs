using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DteConstants = EnvDTE.Constants;
using Morris.FeatureExplorer.Models;

namespace Morris.FeatureExplorer
{
	public sealed class FeatureExplorerViewModel
	{
		public ObservableCollection<NodeBase> RootNodes { get; } = new ObservableCollection<NodeBase>();

		private const string FeaturesFolderName = "Features";
		private DTE2 Dte;
		private FolderNode VirtualRoot = new FolderNode(string.Empty);

		public void AddItem(string fullPath, bool isFolder)
		{
			string[] segments = GetFeatureRelativeSegments(fullPath);
			if (segments == null || segments.Length == 0)
				return;

			FolderNode parent = EnsureFolderPath(segments, 0, segments.Length - 1, fullPath);
			string leafName = segments[segments.Length - 1];

			if (isFolder)
			{
				FolderNode existing = parent.Children
					.OfType<FolderNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, leafName));

				if (existing != null)
				{
					existing.SourcePaths.Add(fullPath);
				}
				else
				{
					var node = new FolderNode(leafName);
					node.SourcePaths.Add(fullPath);
					parent.Children.AddSorted(node);
				}
			}
			else
			{
				FileNode existing = parent.Children
					.OfType<FileNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, leafName));

				if (existing != null)
				{
					existing.SourcePaths.Add(fullPath);
				}
				else
				{
					var node = new FileNode(leafName);
					node.SourcePaths.Add(fullPath);
					parent.Children.AddSorted(node);
				}
			}
		}

		public void Clear()
		{
			RootNodes.Clear();
			VirtualRoot = new FolderNode(string.Empty);
		}

		public void ClearDte()
		{
			Dte = null;
			Clear();
		}

		public void RebuildTree()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			RootNodes.Clear();
			VirtualRoot = new FolderNode(string.Empty);

			DTE2 dte = Dte;
			if (dte?.Solution == null)
				return;

			foreach (Project project in dte.Solution.Projects)
				ScanProject(project, VirtualRoot);

			foreach (NodeBase node in VirtualRoot.Children)
				RootNodes.Add(node);
		}

		public void RemoveItem(string fullPath, bool isFolder)
		{
			string[] segments = GetFeatureRelativeSegments(fullPath);
			if (segments == null || segments.Length == 0)
				return;

			FolderNode parent = FindFolderPath(segments, 0, segments.Length - 1);
			if (parent == null)
				return;

			string leafName = segments[segments.Length - 1];
			NodeBase target = isFolder
				? (NodeBase)parent.Children.OfType<FolderNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, leafName))
				: parent.Children.OfType<FileNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, leafName));

			if (target == null)
				return;

			target.SourcePaths.Remove(fullPath);
			if (target.SourcePaths.Count == 0)
				parent.Children.Remove(target);

			PruneEmptyFolders(segments, segments.Length - 2);
		}

		public void RenameItem(string oldPath, string newPath, bool isFolder)
		{
			string[] oldSegments = GetFeatureRelativeSegments(oldPath);
			string[] newSegments = GetFeatureRelativeSegments(newPath);

			if (oldSegments == null && newSegments == null)
				return;

			if (oldSegments == null)
			{
				AddItem(newPath, isFolder);
				return;
			}

			if (newSegments == null)
			{
				RemoveItem(oldPath, isFolder);
				return;
			}

			if (!isFolder)
			{
				RemoveItem(oldPath, isFolder: false);
				AddItem(newPath, isFolder: false);
				return;
			}

			RenameFolderIncremental(oldPath, newPath, oldSegments, newSegments);
		}

		public void SelectInSolutionExplorer(FileNode node)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (Dte == null || node.SourcePaths.Count == 0)
				return;

			string path = node.SourcePaths.First();

			try
			{
				ProjectItem projectItem = Dte.Solution.FindProjectItem(path);
				if (projectItem == null)
					return;

				var serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Dte;

				var solution = (IVsSolution)GetService(serviceProvider, typeof(SVsSolution), typeof(IVsSolution));
				if (solution == null)
					return;

				if (!ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(projectItem.ContainingProject.UniqueName, out IVsHierarchy hierarchy))
					|| hierarchy == null)
					return;

				string fullPath = projectItem.get_FileNames(1);
				if (!(hierarchy is IVsProject vsProject)
					|| !ErrorHandler.Succeeded(vsProject.IsDocumentInProject(fullPath, out int found, new VSDOCUMENTPRIORITY[1], out uint itemId))
					|| found == 0)
					return;

				var shell = (IVsUIShell)GetService(serviceProvider, typeof(SVsUIShell), typeof(IVsUIShell));
				if (shell == null)
					return;

				Guid solutionExplorerGuid = new Guid(ToolWindowGuids.SolutionExplorer);
				if (!ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref solutionExplorerGuid, out IVsWindowFrame seFrame))
					|| seFrame == null)
					return;

				if (ErrorHandler.Succeeded(seFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView))
					&& docView is IVsUIHierarchyWindow hierarchyWindow)
				{
					hierarchyWindow.ExpandItem(hierarchy as IVsUIHierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
				}

				// Briefly show SE so it pushes selection to Properties, then reactivate FE
				seFrame.ShowNoActivate();
			}
			catch
			{
				// Item may not be accessible
			}
		}

		public void SetDte(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Dte = dte;
			RebuildTree();
		}

		internal static string[] GetFeatureRelativeSegments(string fullPath)
		{
			if (string.IsNullOrEmpty(fullPath))
				return null;

			string[] allSegments = fullPath.Split(
				new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
				StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < allSegments.Length; i++)
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(allSegments[i], FeaturesFolderName))
				{
					int remaining = allSegments.Length - i - 1;
					if (remaining == 0)
						return null;

					var result = new string[remaining];
					Array.Copy(allSegments, i + 1, result, 0, remaining);
					return result;
				}
			}

			return null;
		}

		private FolderNode EnsureFolderPath(string[] segments, int start, int endExclusive, string sourcePath)
		{
			FolderNode current = VirtualRoot;
			for (int i = start; i < endExclusive; i++)
			{
				string name = segments[i];
				FolderNode child = current.Children
					.OfType<FolderNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, name));

				if (child == null)
				{
					child = new FolderNode(name);
					child.SourcePaths.Add(sourcePath);
					current.Children.AddSorted(child);
					SyncRootNodes(current);
				}
				else
				{
					child.SourcePaths.Add(sourcePath);
				}

				current = child;
			}
			return current;
		}

		private FolderNode FindFolderPath(string[] segments, int start, int endExclusive)
		{
			FolderNode current = VirtualRoot;
			for (int i = start; i < endExclusive; i++)
			{
				string name = segments[i];
				FolderNode child = current.Children
					.OfType<FolderNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, name));

				if (child == null)
					return null;

				current = child;
			}
			return current;
		}

		private static object GetService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Type serviceType, Type interfaceType)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Guid serviceGuid = serviceType.GUID;
			Guid interfaceGuid = interfaceType.GUID;
			if (ErrorHandler.Succeeded(serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid, out IntPtr obj)) && obj != IntPtr.Zero)
			{
				try
				{
					return System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(obj);
				}
				finally
				{
					System.Runtime.InteropServices.Marshal.Release(obj);
				}
			}
			return null;
		}

		private void MergeFolder(ProjectItem folderItem, FolderNode parentNode)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (folderItem.ProjectItems == null)
				return;

			foreach (ProjectItem child in folderItem.ProjectItems)
			{
				try
				{
					string childName = child.Name;
					string childKind = child.Kind;

					string childPath;
					try { childPath = child.get_FileNames(1); }
					catch { childPath = childName; }

					if (childKind == DteConstants.vsProjectItemKindPhysicalFolder)
					{
						FolderNode existingFolder = parentNode.Children
							.OfType<FolderNode>()
							.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, childName));

						if (existingFolder == null)
						{
							existingFolder = new FolderNode(childName);
							parentNode.Children.AddSorted(existingFolder);
						}

						existingFolder.SourcePaths.Add(childPath);
						MergeFolder(child, existingFolder);
					}
					else if (childKind == DteConstants.vsProjectItemKindPhysicalFile)
					{
						FileNode existingFile = parentNode.Children
							.OfType<FileNode>()
							.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, childName));

						if (existingFile == null)
						{
							existingFile = new FileNode(childName);
							parentNode.Children.AddSorted(existingFile);
						}

						existingFile.SourcePaths.Add(childPath);
					}
				}
				catch (Exception)
				{
					// Item may be inaccessible
				}
			}
		}

		private static void MoveMatchingChildren(FolderNode source, FolderNode target, string oldPathPrefix, string newPathPrefix)
		{
			for (int i = source.Children.Count - 1; i >= 0; i--)
			{
				NodeBase child = source.Children[i];

				var matchingPaths = child.SourcePaths
					.Where(p => p.StartsWith(oldPathPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (matchingPaths.Count == 0)
					continue;

				var newPaths = matchingPaths
					.Select(p => newPathPrefix + p.Substring(oldPathPrefix.Length))
					.ToList();

				foreach (string path in matchingPaths)
					child.SourcePaths.Remove(path);

				if (child is FolderNode childFolder)
				{
					FolderNode targetChild = target.Children
						.OfType<FolderNode>()
						.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, child.Name));

					if (targetChild == null)
					{
						if (child.SourcePaths.Count == 0)
						{
							source.Children.RemoveAt(i);
							foreach (string path in newPaths)
								child.SourcePaths.Add(path);
							UpdateDescendantSourcePaths(childFolder, oldPathPrefix, newPathPrefix);
							target.Children.AddSorted(child);
							continue;
						}

						targetChild = new FolderNode(child.Name);
						target.Children.AddSorted(targetChild);
					}

					foreach (string path in newPaths)
						targetChild.SourcePaths.Add(path);

					MoveMatchingChildren(childFolder, targetChild, oldPathPrefix, newPathPrefix);

					if (child.SourcePaths.Count == 0 && childFolder.Children.Count == 0)
						source.Children.RemoveAt(i);
				}
				else
				{
					FileNode targetChild = target.Children
						.OfType<FileNode>()
						.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, child.Name));

					if (targetChild == null)
					{
						if (child.SourcePaths.Count == 0)
						{
							source.Children.RemoveAt(i);
							foreach (string path in newPaths)
								child.SourcePaths.Add(path);
							target.Children.AddSorted(child);
							continue;
						}

						targetChild = new FileNode(child.Name);
						target.Children.AddSorted(targetChild);
					}

					foreach (string path in newPaths)
						targetChild.SourcePaths.Add(path);

					if (child.SourcePaths.Count == 0)
						source.Children.RemoveAt(i);
				}
			}
		}

		private void PruneEmptyFolders(string[] segments, int lastFolderIndex)
		{
			for (int i = lastFolderIndex; i >= 0; i--)
			{
				FolderNode parent = i == 0 ? VirtualRoot : FindFolderPath(segments, 0, i);
				if (parent == null)
					break;

				string name = segments[i];
				FolderNode folder = parent.Children
					.OfType<FolderNode>()
					.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, name));

				if (folder != null && folder.Children.Count == 0 && folder.SourcePaths.Count == 0)
				{
					parent.Children.Remove(folder);
					SyncRootNodes(parent);
				}
				else
				{
					break;
				}
			}
		}

		private void RenameFolderIncremental(string oldPath, string newPath, string[] oldSegments, string[] newSegments)
		{
			FolderNode oldParent = FindFolderPath(oldSegments, 0, oldSegments.Length - 1);
			if (oldParent == null)
				return;

			string oldLeaf = oldSegments[oldSegments.Length - 1];
			FolderNode sourceNode = oldParent.Children
				.OfType<FolderNode>()
				.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, oldLeaf));

			if (sourceNode == null)
				return;

			FolderNode newParent = EnsureFolderPath(newSegments, 0, newSegments.Length - 1, newPath);
			string newLeaf = newSegments[newSegments.Length - 1];
			FolderNode targetNode = newParent.Children
				.OfType<FolderNode>()
				.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, newLeaf));

			if (targetNode == null)
			{
				targetNode = new FolderNode(newLeaf);
				newParent.Children.AddSorted(targetNode);
				SyncRootNodes(newParent);
			}

			targetNode.SourcePaths.Add(newPath);
			MoveMatchingChildren(sourceNode, targetNode, oldPath, newPath);

			sourceNode.SourcePaths.Remove(oldPath);
			if (sourceNode.SourcePaths.Count == 0 && sourceNode.Children.Count == 0)
			{
				oldParent.Children.Remove(sourceNode);
				SyncRootNodes(oldParent);
			}
		}

		private void ScanProject(Project project, FolderNode mergeTarget)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project == null)
				return;

			try
			{
				if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
				{
					foreach (ProjectItem item in project.ProjectItems)
					{
						if (item.SubProject != null)
							ScanProject(item.SubProject, mergeTarget);
					}
					return;
				}

				ProjectItems items = project.ProjectItems;
				if (items == null)
					return;

				foreach (ProjectItem item in items)
				{
					if (StringComparer.OrdinalIgnoreCase.Equals(item.Name, FeaturesFolderName)
						&& item.Kind == DteConstants.vsProjectItemKindPhysicalFolder)
					{
						MergeFolder(item, mergeTarget);
					}
				}
			}
			catch (Exception)
			{
				// Project may be unloaded or inaccessible
			}
		}

		private static bool SegmentPrefixEquals(string[] a, string[] b, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!StringComparer.OrdinalIgnoreCase.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		private void SyncRootNodes(FolderNode possibleRoot)
		{
			if (possibleRoot != VirtualRoot)
				return;

			RootNodes.Clear();
			foreach (NodeBase node in VirtualRoot.Children)
				RootNodes.Add(node);
		}

		private static void UpdateDescendantSourcePaths(FolderNode folder, string oldPrefix, string newPrefix)
		{
			foreach (NodeBase child in folder.Children)
			{
				var updated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string path in child.SourcePaths)
				{
					if (path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
						updated.Add(newPrefix + path.Substring(oldPrefix.Length));
					else
						updated.Add(path);
				}
				child.SourcePaths.Clear();
				foreach (string path in updated)
					child.SourcePaths.Add(path);

				if (child is FolderNode childFolder)
					UpdateDescendantSourcePaths(childFolder, oldPrefix, newPrefix);
			}
		}
	}
}
