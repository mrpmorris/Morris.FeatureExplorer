using System;
using System.Collections;
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
		private Models.FileNode PendingRenameNode;
		private ITrackSelection TrackSelection;
		private IVsTrackSelectionEx TrackSelectionEx;

		public FeatureExplorerToolWindow() : base(null)
		{
			Caption = "Feature Explorer 7";
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

			var menuGuid = new Guid("D309F791-903F-11D0-9EFC-00A0C911004F");
			var points = new POINTS[]
			{
				new POINTS
				{
					x = (short)screenPoint.X,
					y = (short)screenPoint.Y
				}
			};
			shell.ShowContextMenu(0, ref menuGuid, 0x0431, points, cmdTarget);
		}

		public override void OnToolWindowCreated()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			base.OnToolWindowCreated();
			EnsureTrackSelection();

			var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
			if (dte != null)
			{
				CommandEvents = dte.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 150];
				CommandEvents.BeforeExecute += OnBeforeRenameExecute;
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
	}
}
