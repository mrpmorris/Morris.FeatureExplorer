using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	internal sealed class SolutionEventsListener : IVsSolutionEvents, IDisposable
	{
		private readonly FeatureExplorerViewModel _viewModel;
		private IVsSolution _solution;
		private uint _cookie;
		private bool _solutionClosing;

		public SolutionEventsListener(IVsSolution solution, FeatureExplorerViewModel viewModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_solution = solution;
			_viewModel = viewModel;
			_solution.AdviseSolutionEvents(this, out _cookie);
		}

		public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_solutionClosing = false;
			_viewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseSolution(object pUnkReserved)
		{
			_solutionClosing = true;
			_viewModel.Clear();
			return VSConstants.S_OK;
		}

		public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_viewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (!_solutionClosing)
				_viewModel.RebuildTree();
			return VSConstants.S_OK;
		}

		public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
		public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
		public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
		public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;
		public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
		public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (_cookie != 0 && _solution != null)
			{
				_solution.UnadviseSolutionEvents(_cookie);
				_cookie = 0;
				_solution = null;
			}
		}
	}
}
