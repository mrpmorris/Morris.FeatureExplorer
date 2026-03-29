using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Models;

namespace Morris.FeatureExplorer
{
	public sealed class FeatureExplorerViewModel
	{
		private const string FeaturesFolderName = "Features";

		private DTE2 _dte;

		public ObservableCollection<NodeBase> RootNodes { get; } = new ObservableCollection<NodeBase>();

		public void SetDte(DTE2 dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_dte = dte;
			RebuildTree();
		}

		public void RebuildTree()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			RootNodes.Clear();
			_virtualRoot = new FolderNode(string.Empty);

			DTE2 dte = _dte;
			if (dte?.Solution == null)
				return;

			foreach (Project project in dte.Solution.Projects)
				ScanProject(project, _virtualRoot);

			foreach (NodeBase node in _virtualRoot.Children)
				RootNodes.Add(node);
		}

		public void Clear()
		{
			RootNodes.Clear();
			_virtualRoot = new FolderNode(string.Empty);
		}

		public void ClearDte()
		{
			_dte = null;
			Clear();
		}

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

			// Step 1: Find or create target folder
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

			// Step 2 & 3: Move children belonging to this project (matching oldPath prefix)
			targetNode.SourcePaths.Add(newPath);
			MoveMatchingChildren(sourceNode, targetNode, oldPath, newPath);

			// Step 4: Remove oldPath from source, remove source if empty
			sourceNode.SourcePaths.Remove(oldPath);
			if (sourceNode.SourcePaths.Count == 0 && sourceNode.Children.Count == 0)
			{
				oldParent.Children.Remove(sourceNode);
				SyncRootNodes(oldParent);
			}
		}

		private static void MoveMatchingChildren(FolderNode source, FolderNode target, string oldPathPrefix, string newPathPrefix)
		{
			for (int i = source.Children.Count - 1; i >= 0; i--)
			{
				NodeBase child = source.Children[i];

				// Find source paths belonging to the renamed project
				var matchingPaths = child.SourcePaths
					.Where(p => p.StartsWith(oldPathPrefix, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (matchingPaths.Count == 0)
					continue;

				// Compute new paths
				var newPaths = matchingPaths
					.Select(p => newPathPrefix + p.Substring(oldPathPrefix.Length))
					.ToList();

				// Remove old paths from source child
				foreach (string path in matchingPaths)
					child.SourcePaths.Remove(path);

				if (child is FolderNode childFolder)
				{
					// Find or create matching folder in target
					FolderNode targetChild = target.Children
						.OfType<FolderNode>()
						.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, child.Name));

					if (targetChild == null)
					{
						if (child.SourcePaths.Count == 0)
						{
							// No other projects reference this folder — move the whole node
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

					// Recurse into subfolder
					MoveMatchingChildren(childFolder, targetChild, oldPathPrefix, newPathPrefix);

					// Remove source folder if empty
					if (child.SourcePaths.Count == 0 && childFolder.Children.Count == 0)
						source.Children.RemoveAt(i);
				}
				else
				{
					// File node
					FileNode targetChild = target.Children
						.OfType<FileNode>()
						.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, child.Name));

					if (targetChild == null)
					{
						if (child.SourcePaths.Count == 0)
						{
							// No other projects reference this file — move the whole node
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

					// Remove source file if empty
					if (child.SourcePaths.Count == 0)
						source.Children.RemoveAt(i);
				}
			}
		}

		private FolderNode EnsureFolderPath(string[] segments, int start, int endExclusive, string sourcePath)
		{
			FolderNode current = _virtualRoot;
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
			FolderNode current = _virtualRoot;
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

		private void PruneEmptyFolders(string[] segments, int lastFolderIndex)
		{
			for (int i = lastFolderIndex; i >= 0; i--)
			{
				FolderNode parent = i == 0 ? _virtualRoot : FindFolderPath(segments, 0, i);
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

		private FolderNode _virtualRoot = new FolderNode(string.Empty);

		private void SyncRootNodes(FolderNode possibleRoot)
		{
			if (possibleRoot != _virtualRoot)
				return;

			RootNodes.Clear();
			foreach (NodeBase node in _virtualRoot.Children)
				RootNodes.Add(node);
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

		private static bool SegmentPrefixEquals(string[] a, string[] b, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!StringComparer.OrdinalIgnoreCase.Equals(a[i], b[i]))
					return false;
			}
			return true;
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
						&& item.Kind == Constants.vsProjectItemKindPhysicalFolder)
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

					if (childKind == Constants.vsProjectItemKindPhysicalFolder)
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
					else if (childKind == Constants.vsProjectItemKindPhysicalFile)
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
	}
}
