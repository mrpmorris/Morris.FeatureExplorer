using System;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	[Guid("B7E3A1F0-5C2D-4E8A-9F1B-3D6C7E8F9A0B")]
	public sealed class FeatureExplorerToolWindow : ToolWindowPane
	{
		private bool Initialized;
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

		public override void OnToolWindowCreated()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			base.OnToolWindowCreated();
			EnsureTrackSelection();
		}

		private void EnsureTrackSelection()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (TrackSelection == null)
				TrackSelection = GetService(typeof(STrackSelection)) as ITrackSelection;
			if (TrackSelectionEx == null)
				TrackSelectionEx = GetService(typeof(STrackSelection)) as IVsTrackSelectionEx;
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
