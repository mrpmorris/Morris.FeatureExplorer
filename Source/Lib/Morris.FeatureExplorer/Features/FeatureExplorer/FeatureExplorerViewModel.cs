#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Features.FeatureExplorer.Models;

internal sealed class FeatureExplorerViewModel : IDisposable, IObserver<IEnumerable<IProjectItemUpdate<IEntityWithId>>>
{
	public static FeatureExplorerViewModel Instance { get; private set; } = null!;
	public SortedObservableCollection<NodeBase> RootNodes => RootNode.ChildNodes;

	private readonly object SyncRoot = new object();
	private readonly FolderNode RootNode;
	private readonly Dictionary<string, NodeBase> NodesByFullPath = new();
	private readonly Dictionary<Guid, ProjectInfo> ProjectInfoByGuid = new();
	private SolutionProjectEventsObserver SolutionProjectEventsObserver;
	private IDisposable ProjectItemsSubscription;

	private FeatureExplorerViewModel()
	{
		RootNode = new FolderNode(name: "Features", relativePath: Consts.FeaturesPath);
		NodesByFullPath[RootNode.RelativePath] = RootNode;
	}

	public static async ValueTask<FeatureExplorerViewModel> CreateAsync(AsyncPackage package)
	{
		if (Instance is null)
			Instance = await CreateInstanceAsync(package);
		return Instance;
	}

	void IDisposable.Dispose()
	{
		((IDisposable)SolutionProjectEventsObserver).Dispose();
		ProjectItemsSubscription?.Dispose();
	}

	void IObserver<IEnumerable<IProjectItemUpdate<IEntityWithId>>>.OnNext(IEnumerable<IProjectItemUpdate<IEntityWithId>> updates)
	{
		ThreadHelper.JoinableTaskFactory.Run(async () =>
		{
			Guid projectGuid = Guid.NewGuid();
			string projectFeaturesFolderPath = null;
			foreach (IProjectItemUpdate<IEntityWithId> update in updates)
			{
				System.Diagnostics.Trace.WriteLine($"[FeatureExplorer] Proj: {update.ProjectGuid}, Change: {update.UpdateType}, PrevId: {update.PreviousId}, NewId: {update.Current?.Id}");
				// Get the path before removing the project
				if (update is IProjectItemUpdate<IProjectSnapshot> removedProject && removedProject.UpdateType == UpdateType.Removed)
				{
					getOrAddProjectFeaturesFolderPath(ref projectGuid, ref projectFeaturesFolderPath, update);
					HandleProjectRemoval(removedProject);
				}
				else if (update is IProjectItemUpdate<IProjectSnapshot> project)
				{
					// Get the path after adding or updating the project
					if (project.UpdateType is UpdateType.Added)
						HandleProjectAddition(project);
					else if (project.UpdateType is UpdateType.Updated)
						HandleProjectUpdate(project);
					getOrAddProjectFeaturesFolderPath(ref projectGuid, ref projectFeaturesFolderPath, update);
				}
				else if (update is IProjectItemUpdate<IFileSnapshot> file)
				{
					if (file.UpdateType is UpdateType.Added)
						HandleFileAddition(file);
					else if (file.UpdateType is UpdateType.Updated)
						HandleFileUpdate(file);
					else if (file.UpdateType == UpdateType.Removed)
						HandleFileRemoval(file);
				}
				else if (update is IProjectItemUpdate<IFolderSnapshot> folder)
				{
					if (folder.UpdateType is UpdateType.Added)
						HandleFolderAddition(folder, out _, out _);
					else if (folder.UpdateType is UpdateType.Updated)
						HandleFolderUpdate(folder);
					else if (folder.UpdateType == UpdateType.Removed)
						HandleFolderRemoval(folder, out _, out _);
				}
			}

			void getOrAddProjectFeaturesFolderPath(ref Guid projectGuid, ref string projectFeaturesFolderPath, IProjectItemUpdate<IEntityWithId> update)
			{
				if (update.ProjectGuid != projectGuid)
				{
					projectFeaturesFolderPath = ProjectInfoByGuid[update.ProjectGuid].FeaturesFolderPath;
					projectGuid = update.ProjectGuid;
				}
			}
		});
	}

	private void HandleProjectAddition(IProjectItemUpdate<IProjectSnapshot> project)
	{
		string featuresFolderPath =
			Path.Combine(
				Path.GetDirectoryName(project.Current.Path),
				Consts.FeaturesPath
			);
		var projectInfo =
			new ProjectInfo(
				projectGuid: project.ProjectGuid,
				featuresFolderPath: featuresFolderPath,
				name: Path.GetFileName(project.Current.Id.GetProjectPath())
			);
		ProjectInfoByGuid[project.ProjectGuid] = projectInfo;
	}

	private void HandleProjectUpdate(IProjectItemUpdate<IProjectSnapshot> project)
	{
		HandleProjectRemoval(project);
		HandleProjectAddition(project);
	}

	private void HandleProjectRemoval(IProjectItemUpdate<IProjectSnapshot> project)
	{
		ProjectInfoByGuid.Remove(project.ProjectGuid);
		UnloadProject(project.ProjectGuid, RootNode);
	}

	private void UnloadProject(Guid projectGuid, NodeBase node)
	{
		if (node is FolderNode folderNode)
		{
			for (int i = folderNode.ChildNodes.Count - 1; i >= 0; i--)
			{
				NodeBase childNode = folderNode.ChildNodes[i];
				UnloadProject(projectGuid, childNode);
				if (!childNode.Projects.Any())
					folderNode.ChildNodes.RemoveAt(i);
			}
		}
		node.Projects.Remove(projectGuid);
		if (!node.Projects.Any())
			NodesByFullPath.Remove(node.RelativePath);
	}

	private void HandleFolderAddition(
		IProjectItemUpdate<IFolderSnapshot> folder,
		out FolderNode folderNode,
		out bool wasNewNode)
	{
		string folderPath = folder.GetFullPath();
		if (!folderPath.StartsWith(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase))
		{
			folderNode = null;
			wasNewNode = false;
			return;
		}

		if (NodesByFullPath.TryGetValue(folderPath, out NodeBase existingNode))
		{
			folderNode = (FolderNode)existingNode;
			wasNewNode = false;
		}
		else
		{
			wasNewNode = true;
			folderNode =
				new FolderNode(
					name: folder.Current.Name,
					relativePath: folder.Current.RelativePath
				);

			NodesByFullPath[folderPath] = folderNode;

			if (TryGetParentNode(folderNode, out FolderNode parentFolderNode))
				parentFolderNode.ChildNodes.Add(folderNode);
		}
		folderNode.Projects.Add(folder.ProjectGuid);
	}

	private void HandleFolderUpdate(IProjectItemUpdate<IFolderSnapshot> folder)
	{
		HandleFolderRemoval(folder, out FolderNode removedNode, out bool nodeWasRemoved);
		HandleFolderAddition(folder, out FolderNode addedNode, out bool wasNewNode);
		if (wasNewNode && removedNode is not null && addedNode is not null)
			addedNode.IsExpanded = removedNode.IsExpanded;
	}

	private void HandleFolderRemoval(
		IProjectItemUpdate<IFolderSnapshot> folder,
		out FolderNode folderNode,
		out bool nodeWasRemoved)
	{
		string previousFullPath = folder.GetPreviousFullPath();
		if (
			!previousFullPath.StartsWith(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase)
			|| !NodesByFullPath.TryGetValue(previousFullPath, out NodeBase existingNode))
		{
			folderNode = null;
			nodeWasRemoved = false;
			return;
		}

		nodeWasRemoved = false;
		folderNode = (FolderNode)existingNode;
		folderNode.Projects.Remove(folder.ProjectGuid);
		if (folderNode.Projects.Count == 0)
		{
			nodeWasRemoved = true;
			if (TryGetParentNode(folderNode, out FolderNode parentFolderNode))
			{ 
				parentFolderNode.ChildNodes.Remove(folderNode);
			}
			NodesByFullPath.Remove(previousFullPath);
		}
	}

	private void HandleFileAddition(IProjectItemUpdate<IFileSnapshot> file)
	{
		string fileFullPath = file.GetFullPath();
		if (!fileFullPath.StartsWith(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase))
			return;

		FileNode fileNode;

		if (NodesByFullPath.TryGetValue(file.Current.ItemName, out NodeBase existingNode))
			fileNode = (FileNode)existingNode;
		else
		{
			fileNode =
				new FileNode(
					name: file.Current.FileName,
					relativePath: file.Current.ItemName
				);

			NodesByFullPath[fileNode.RelativePath] = fileNode;

			if (TryGetParentNode(fileNode, out FolderNode parentFolderNode))
			{
				parentFolderNode.ChildNodes.Add(fileNode);
			}
		}
		fileNode.Projects.Add(file.ProjectGuid);
	}

	private void HandleFileUpdate(IProjectItemUpdate<IFileSnapshot> file)
	{
		HandleFileRemoval(file);
		HandleFileAddition(file);
	}

	private void HandleFileRemoval(IProjectItemUpdate<IFileSnapshot> file)
	{
		string previousFullPath = file.GetPreviousFullPath();
		if (!previousFullPath.StartsWith(Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase))
			return;

		if (!NodesByFullPath.TryGetValue(previousFullPath, out NodeBase existingNode))
			return;


		var fileNode = (FileNode)existingNode;
		fileNode.Projects.Remove(file.ProjectGuid);
		if (fileNode.Projects.Count == 0)
		{
			if (TryGetParentNode(fileNode, out FolderNode parentFolderNode))
			{
				parentFolderNode.ChildNodes.Remove(fileNode);
			}
			NodesByFullPath.Remove(previousFullPath);
		}
	}

	private static async ValueTask<FeatureExplorerViewModel> CreateInstanceAsync(AsyncPackage package)
	{
		var instance = new FeatureExplorerViewModel();
		await instance.InitializeAsync(package);
		return instance;
	}

	private bool TryGetParentNode(NodeBase node, out FolderNode parentNode)
	{
		parentNode = null;
		string parentPath = node.ParentPath;
		if (parentPath is null || string.Equals(parentPath, Consts.FeaturesPath, StringComparison.OrdinalIgnoreCase))
		{
			parentNode = RootNode;
			return true;
		}

		NodesByFullPath.TryGetValue(parentPath, out NodeBase result);
		parentNode = (FolderNode)result;
		return parentNode is not null;
	}

	private async ValueTask InitializeAsync(AsyncPackage package)
	{
		((IDisposable)SolutionProjectEventsObserver)?.Dispose();

		SolutionProjectEventsObserver = await SolutionProjectEventsObserver.CreateAsync(package);
		ProjectItemsSubscription = SolutionProjectEventsObserver.Observable.Subscribe(this);
	}

	#region Unused
	void IObserver<IEnumerable<IProjectItemUpdate<IEntityWithId>>>.OnCompleted()
	{
	}

	void IObserver<IEnumerable<IProjectItemUpdate<IEntityWithId>>>.OnError(Exception error)
	{
	}
	#endregion

}