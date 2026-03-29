#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Extensions;
using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal class FeatureExplorerViewModel
{
	public static FeatureExplorerViewModel Instance { get; private set; }

	public SortedObservableCollection<NodeBase> RootNodes { get; } =
		new(new NodeBaseComparer());

	private readonly Dictionary<string, NodeBase> NodesByRelativePath = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<Guid, ProjectInfo> ProjectInfoByGuid = new();

	public static async ValueTask<FeatureExplorerViewModel> CreateAsync(AsyncPackage package)
	{
		var viewModel = new FeatureExplorerViewModel();
		Instance = viewModel;

		var observer = await SolutionProjectEventsObserver.CreateAsync(package);
		observer.BatchedUpdates.Subscribe(viewModel.ProcessBatch);

		return viewModel;
	}

	private FeatureExplorerViewModel() { }

	private void ProcessBatch(IList<IProjectItemUpdate<IEntityWithId>> batch)
	{
		var processed = batch
			.CombineRemoveAndAddToChange()
			.SortByProcessingOrder();

		foreach (var update in processed)
		{
			switch (update.Current)
			{
				case IProjectSnapshot project:
					HandleProjectUpdate(update.ProjectGuid, project, update.UpdateType);
					break;

				case IFolderSnapshot folder:
					HandleFolderUpdate(update.ProjectGuid, folder, update.UpdateType, update.PreviousId);
					break;

				case IFileSnapshot file:
					HandleFileUpdate(update.ProjectGuid, file, update.UpdateType, update.PreviousId);
					break;
			}
		}
	}

	private void HandleProjectUpdate(Guid projectGuid, IProjectSnapshot project, UpdateType updateType)
	{
		switch (updateType)
		{
			case UpdateType.Added:
			case UpdateType.Updated:
				ProjectInfoByGuid[projectGuid] = new ProjectInfo(
					projectGuid,
					project.Name,
					project.Path);
				break;

			case UpdateType.Removed:
				ProjectInfoByGuid.Remove(projectGuid);
				RemoveProjectFromAllNodes(projectGuid);
				break;
		}
	}

	private void HandleFolderUpdate(Guid projectGuid, IFolderSnapshot folder, UpdateType updateType, EntityIdentity previousId)
	{
		string relativePath = folder.RelativePath;
		if (!IsUnderFeaturesFolder(relativePath))
			return;

		string featureRelativePath = GetFeatureRelativePath(relativePath);

		switch (updateType)
		{
			case UpdateType.Added:
				EnsureFolderExists(featureRelativePath, folder.Name, projectGuid);
				break;

			case UpdateType.Removed:
				string previousPath = previousId?.GetFolderPath();
				if (previousPath is not null && IsUnderFeaturesFolder(previousPath))
				{
					string prevFeaturePath = GetFeatureRelativePath(previousPath);
					RemoveProjectFromNode(prevFeaturePath, projectGuid);
				}
				break;

			case UpdateType.Updated:
				EnsureFolderExists(featureRelativePath, folder.Name, projectGuid);
				break;
		}
	}

	private void HandleFileUpdate(Guid projectGuid, IFileSnapshot file, UpdateType updateType, EntityIdentity previousId)
	{
		string filePath = file.VisualPath ?? file.Path;
		if (filePath is null)
			return;

		string relativePath = ExtractRelativePathFromFilePath(filePath);
		if (!IsUnderFeaturesFolder(relativePath))
			return;

		string featureRelativePath = GetFeatureRelativePath(relativePath);

		switch (updateType)
		{
			case UpdateType.Added:
				EnsureFileExists(featureRelativePath, file.FileName, projectGuid);
				break;

			case UpdateType.Removed:
				string previousName = previousId?.GetSourceItemName();
				if (previousName is not null)
				{
					string prevPath = previousId.GetFolderPath();
					if (prevPath is not null && IsUnderFeaturesFolder(prevPath))
					{
						string prevFeaturePath = GetFeatureRelativePath(prevPath);
						RemoveProjectFromNode(prevFeaturePath, projectGuid);
					}
				}
				else
				{
					RemoveProjectFromNode(featureRelativePath, projectGuid);
				}
				break;

			case UpdateType.Updated:
				EnsureFileExists(featureRelativePath, file.FileName, projectGuid);
				break;
		}
	}

	private void EnsureFolderExists(string featureRelativePath, string name, Guid projectGuid)
	{
		if (NodesByRelativePath.TryGetValue(featureRelativePath, out var existing))
		{
			existing.Projects.Add(projectGuid);
			return;
		}

		var folderNode = new FolderNode(name, featureRelativePath);
		folderNode.Projects.Add(projectGuid);
		NodesByRelativePath[featureRelativePath] = folderNode;

		string parentPath = GetParentPath(featureRelativePath);
		if (parentPath is null)
		{
			RootNodes.AddSorted(folderNode);
		}
		else
		{
			EnsureParentFolderExists(parentPath, projectGuid);
			if (NodesByRelativePath.TryGetValue(parentPath, out var parentNode) && parentNode is FolderNode parent)
			{
				parent.ChildNodes.AddSorted(folderNode);
			}
		}
	}

	private void EnsureFileExists(string featureRelativePath, string name, Guid projectGuid)
	{
		if (NodesByRelativePath.TryGetValue(featureRelativePath, out var existing))
		{
			existing.Projects.Add(projectGuid);
			return;
		}

		var fileNode = new FileNode(name, featureRelativePath);
		fileNode.Projects.Add(projectGuid);
		NodesByRelativePath[featureRelativePath] = fileNode;

		string parentPath = GetParentPath(featureRelativePath);
		if (parentPath is null)
		{
			RootNodes.AddSorted(fileNode);
		}
		else
		{
			EnsureParentFolderExists(parentPath, projectGuid);
			if (NodesByRelativePath.TryGetValue(parentPath, out var parentNode) && parentNode is FolderNode parent)
			{
				parent.ChildNodes.AddSorted(fileNode);
			}
		}
	}

	private void EnsureParentFolderExists(string parentPath, Guid projectGuid)
	{
		if (NodesByRelativePath.ContainsKey(parentPath))
		{
			NodesByRelativePath[parentPath].Projects.Add(projectGuid);
			return;
		}

		string parentName = Path.GetFileName(parentPath);
		EnsureFolderExists(parentPath, parentName, projectGuid);
	}

	private void RemoveProjectFromNode(string featureRelativePath, Guid projectGuid)
	{
		if (!NodesByRelativePath.TryGetValue(featureRelativePath, out var node))
			return;

		node.Projects.Remove(projectGuid);
		if (node.Projects.Count > 0)
			return;

		NodesByRelativePath.Remove(featureRelativePath);

		if (node is FolderNode folder)
		{
			foreach (var child in folder.ChildNodes.ToList())
				RemoveProjectFromNode(child.RelativePath, projectGuid);
		}

		string parentPath = GetParentPath(featureRelativePath);
		if (parentPath is null)
		{
			RootNodes.Remove(node);
		}
		else if (NodesByRelativePath.TryGetValue(parentPath, out var parentNode) && parentNode is FolderNode parentFolder)
		{
			parentFolder.ChildNodes.Remove(node);
		}
	}

	private void RemoveProjectFromAllNodes(Guid projectGuid)
	{
		var paths = NodesByRelativePath.Keys.ToList();
		foreach (string path in paths)
		{
			if (NodesByRelativePath.TryGetValue(path, out var node))
			{
				node.Projects.Remove(projectGuid);
				if (node.Projects.Count == 0)
				{
					NodesByRelativePath.Remove(path);
					string parentPath = GetParentPath(path);
					if (parentPath is null)
					{
						RootNodes.Remove(node);
					}
					else if (NodesByRelativePath.TryGetValue(parentPath, out var parentNode) && parentNode is FolderNode parentFolder)
					{
						parentFolder.ChildNodes.Remove(node);
					}
				}
			}
		}
	}

	private static bool IsUnderFeaturesFolder(string path)
	{
		return path is not null &&
			path.StartsWith(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase);
	}

	private static string GetFeatureRelativePath(string fullRelativePath)
	{
		return fullRelativePath.Substring(Consts.FeaturesPath.Length);
	}

	private static string GetParentPath(string path)
	{
		int lastSep = path.LastIndexOf(Path.DirectorySeparatorChar);
		if (lastSep < 0)
			lastSep = path.LastIndexOf(Path.AltDirectorySeparatorChar);
		return lastSep > 0 ? path.Substring(0, lastSep) : null;
	}

	private static string ExtractRelativePathFromFilePath(string filePath)
	{
		if (filePath is null)
			return null;

		int featuresIndex = filePath.IndexOf(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase);
		if (featuresIndex < 0)
			return filePath;

		return filePath.Substring(featuresIndex);
	}
}
