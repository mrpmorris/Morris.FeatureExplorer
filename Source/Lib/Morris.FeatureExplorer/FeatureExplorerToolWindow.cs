using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	[Guid("B7E3A1F0-5C2D-4E8A-9F1B-3D6C7E8F9A0B")]
	public sealed class FeatureExplorerToolWindow : ToolWindowPane
	{
		private EnvDTE.CommandEvents CommandEvents;
		private bool Initialized;
		private Models.FolderNode PendingFolderContextNode;
		private Models.NodeBase PendingRenameNode;
		private ITrackSelection TrackSelection;
		private IVsTrackSelectionEx TrackSelectionEx;

		public FeatureExplorerToolWindow() : base(null)
		{
			Caption = "Feature Explorer 2";
			Content = new FeatureExplorerToolWindowControl(this);
		}

		public void NotifySelectionChanged(IVsHierarchy hierarchy, uint itemId)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			EnsureTrackSelection();
			if (TrackSelection == null || TrackSelectionEx == null)
				return;

			if (!ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out object browseObject))
				|| browseObject == null)
				return;

			// One-time: briefly show SE to initialize Properties window infrastructure
			if (!Initialized)
			{
				InitializeSolutionExplorer();
				Initialized = true;
			}

			// Ensure our frame is the active selection source
			if (Frame is IVsWindowFrame feFrame)
				feFrame.Show();

			// Set hierarchy context
			IntPtr hierarchyPtr = Marshal.GetIUnknownForObject(hierarchy);
			try
			{
				TrackSelectionEx.OnSelectChangeEx(hierarchyPtr, itemId, null, IntPtr.Zero);
			}
			finally
			{
				Marshal.Release(hierarchyPtr);
			}

			// Set selection container
			var list = new ArrayList { browseObject };
			var container = new SelectionContainer(true, false)
			{
				SelectableObjects = list,
				SelectedObjects = list
			};
			TrackSelection.OnSelectChange(container);
		}

		public void ShowFolderContextMenu(Models.FolderNode folderNode, Point screenPoint)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var shell = GetService(typeof(SVsUIShell)) as IVsUIShell;
			if (shell == null)
				return;

			PendingFolderContextNode = folderNode;

			var cmdTarget = GetService(typeof(IMenuCommandService)) as Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;

			Guid menuGuid = Consts.CommandSetGuid;
			var points = new POINTS[]
			{
				new POINTS
				{
					x = (short)screenPoint.X,
					y = (short)screenPoint.Y
				}
			};
			shell.ShowContextMenu(0, ref menuGuid, Consts.FolderContextMenuId, points, cmdTarget);
		}

		public void ShowItemContextMenu(Models.FileNode fileNode, IVsHierarchy hierarchy, uint itemId, Point screenPoint)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var shell = GetService(typeof(SVsUIShell)) as IVsUIShell;
			if (shell == null)
				return;

			Guid seGuid = new Guid(ToolWindowGuids.SolutionExplorer);
			if (!ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref seGuid, out IVsWindowFrame seFrame))
				|| seFrame == null)
				return;

			Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget cmdTarget = null;
			if (ErrorHandler.Succeeded(seFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView))
				&& docView is IVsUIHierarchyWindow hierarchyWindow
				&& hierarchy is IVsUIHierarchy uiHierarchy)
			{
				hierarchyWindow.ExpandItem(uiHierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
				cmdTarget = docView as Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
			}

			PendingRenameNode = fileNode;

			Guid menuGuid = VsMenus.guidSHLMainMenu;
			var points = new POINTS[]
			{
				new POINTS
				{
					x = (short)screenPoint.X,
					y = (short)screenPoint.Y
				}
			};
			shell.ShowContextMenu(0, ref menuGuid, VsMenus.IDM_VS_CTXT_ITEMNODE, points, cmdTarget);
		}

		public override void OnToolWindowCreated()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			base.OnToolWindowCreated();
			EnsureTrackSelection();

			var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
			if (dte != null)
			{
				CommandEvents = dte.Events.CommandEvents[
					typeof(VSConstants.VSStd97CmdID).GUID.ToString("B"),
					(int)VSConstants.VSStd97CmdID.Rename];
				CommandEvents.BeforeExecute += OnBeforeRenameExecute;
			}

			var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService != null)
			{
				var renameFolderId = new CommandID(Consts.CommandSetGuid, Consts.RenameFolderCommandId);
				var renameFolderCommand = new OleMenuCommand(OnRenameFolderInvoked, renameFolderId);
				renameFolderCommand.BeforeQueryStatus += OnRenameFolderQueryStatus;
				commandService.AddCommand(renameFolderCommand);

				var deleteFolderId = new CommandID(Consts.CommandSetGuid, Consts.DeleteFolderCommandId);
				var deleteFolderCommand = new OleMenuCommand(OnDeleteFolderInvoked, deleteFolderId);
				deleteFolderCommand.BeforeQueryStatus += OnDeleteFolderQueryStatus;
				commandService.AddCommand(deleteFolderCommand);
			}
		}

		private void EnsureTrackSelection()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (TrackSelection == null || TrackSelectionEx == null)
			{
				object service = GetService(typeof(STrackSelection));
				TrackSelection = service as ITrackSelection;
				TrackSelectionEx = service as IVsTrackSelectionEx;
			}
		}

		private void InitializeSolutionExplorer()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var shell = GetService(typeof(SVsUIShell)) as IVsUIShell;
			if (shell == null)
				return;

			Guid seGuid = new Guid(ToolWindowGuids.SolutionExplorer);
			if (ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref seGuid, out IVsWindowFrame seFrame))
				&& seFrame != null)
			{
				seFrame.Show();
				seFrame.Hide();
			}
		}

		private void OnBeforeRenameExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault)
		{
			if (PendingRenameNode != null)
			{
				var node = PendingRenameNode;
				PendingRenameNode = null;
				cancelDefault = true;
				node.IsEditing = true;
			}
		}

		private void OnDeleteFolderInvoked(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (PendingFolderContextNode == null)
				return;

			var node = PendingFolderContextNode;
			PendingFolderContextNode = null;

			if (Content is FeatureExplorerToolWindowControl control)
				control.DeleteFolder(node);
		}

		private void OnDeleteFolderQueryStatus(object sender, EventArgs e)
		{
			if (sender is OleMenuCommand cmd)
			{
				cmd.Visible = true;
				cmd.Enabled = PendingFolderContextNode != null;
			}
		}

		private void OnRenameFolderInvoked(object sender, EventArgs e)
		{
			if (PendingFolderContextNode != null)
			{
				var node = PendingFolderContextNode;
				PendingFolderContextNode = null;
				node.IsEditing = true;
			}
		}

		private void OnRenameFolderQueryStatus(object sender, EventArgs e)
		{
			if (sender is OleMenuCommand cmd)
			{
				cmd.Visible = true;
				cmd.Enabled = PendingFolderContextNode != null;
			}
		}
	}
}
