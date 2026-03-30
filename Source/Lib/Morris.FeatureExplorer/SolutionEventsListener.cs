using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	internal sealed class SolutionEventsListener : IVsSolutionEvents, IDisposable
	{
		private readonly FeatureExplorerViewModel ViewModel;
		private IVsSolution Solution;
		private uint Cookie;
		private bool SolutionClosing;

		public SolutionEventsListener(IVsSolution solution, FeatureExplorerViewModel viewModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Solution = solution;
			ViewModel = viewModel;
			Solution.AdviseSolutionEvents(this, out Cookie);
		}

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (Cookie != 0 && Solution != null)
			{
				Solution.UnadviseSolutionEvents(Cookie);
				Cookie = 0;
				Solution = null;
			}
		}

		public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;

		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			ViewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			SolutionClosing = false;
			ViewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (!SolutionClosing)
				ViewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			SolutionClosing = true;
			ViewModel.Clear();
			return VSConstants.S_OK;
		}

		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
	}
}
