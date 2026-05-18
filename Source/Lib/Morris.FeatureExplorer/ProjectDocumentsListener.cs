using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Morris.FeatureExplorer
{
	internal sealed class ProjectDocumentsListener : IVsTrackProjectDocumentsEvents2, IDisposable
	{
		private readonly FeatureExplorerViewModel ViewModel;
		private IVsTrackProjectDocuments2 Tracker;
		private uint Cookie;

		public ProjectDocumentsListener(IVsTrackProjectDocuments2 tracker, FeatureExplorerViewModel viewModel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Tracker = tracker;
			ViewModel = viewModel;
			Tracker.AdviseTrackProjectDocumentsEvents(this, out Cookie);
		}

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (Cookie != 0 && Tracker != null)
			{
				Tracker.UnadviseTrackProjectDocumentsEvents(Cookie);
				Cookie = 0;
				Tracker = null;
			}
		}

		public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cDirectories; i++)
				ViewModel.AddItem(rgpszMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cFiles; i++)
				ViewModel.AddItem(rgpszMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cDirectories; i++)
				ViewModel.RemoveItem(rgpszMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cFiles; i++)
				ViewModel.RemoveItem(rgpszMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterRenameDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszOldMkDocuments, string[] rgpszNewMkDocuments, VSRENAMEDIRECTORYFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cDirectories; i++)
				ViewModel.RenameItem(rgpszOldMkDocuments[i], rgpszNewMkDocuments[i], isFolder: true);
			return VSConstants.S_OK;
		}

		public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszOldMkDocuments, string[] rgpszNewMkDocuments, VSRENAMEFILEFLAGS[] rgFlags)
		{
			if (ViewModel.SuppressUpdates)
				return VSConstants.S_OK;
			for (int i = 0; i < cFiles; i++)
				ViewModel.RenameItem(rgpszOldMkDocuments[i], rgpszNewMkDocuments[i], isFolder: false);
			return VSConstants.S_OK;
		}

		public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus) => VSConstants.S_OK;
		public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults) => VSConstants.S_OK;
		public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgpszMkOldNames, string[] rgpszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults) => VSConstants.S_OK;
	}
}
