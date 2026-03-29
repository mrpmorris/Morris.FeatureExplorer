using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	internal sealed class ProjectDocumentsListener : IVsTrackProjectDocumentsEvents2, IDisposable
	{
		private readonly FeatureExplorerViewModel _viewModel;
		private IVsTrackProjectDocuments2 _tracker;
		private uint _cookie;

		public ProjectDocumentsListener(IVsTrackProjectDocuments2 tracker, FeatureExplorerViewModel viewModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_tracker = tracker;
			_viewModel = viewModel;
			_tracker.AdviseTrackProjectDocumentsEvents(this, out _cookie);
		}

		public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
		{
			for (int i = 0; i < cFiles; i++)
				_viewModel.AddItem(rgpszMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
		{
			for (int i = 0; i < cDirectories; i++)
				_viewModel.AddItem(rgpszMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
		{
			for (int i = 0; i < cFiles; i++)
				_viewModel.RemoveItem(rgpszMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
		{
			for (int i = 0; i < cDirectories; i++)
				_viewModel.RemoveItem(rgpszMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszOldMkDocuments, string[] rgpszNewMkDocuments, VSRENAMEFILEFLAGS[] rgFlags)
		{
			for (int i = 0; i < cFiles; i++)
				_viewModel.RenameItem(rgpszOldMkDocuments[i], rgpszNewMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterRenameDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszOldMkDocuments, string[] rgpszNewMkDocuments, VSRENAMEDIRECTORYFLAGS[] rgFlags)
		{
			for (int i = 0; i < cDirectories; i++)
				_viewModel.RenameItem(rgpszOldMkDocuments[i], rgpszNewMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus) => VSConstants.S_OK;
		public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgpszMkOldNames, string[] rgpszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (_cookie != 0 && _tracker != null)
			{
				_tracker.UnadviseTrackProjectDocumentsEvents(_cookie);
				_cookie = 0;
				_tracker = null;
			}
		}
	}
}
