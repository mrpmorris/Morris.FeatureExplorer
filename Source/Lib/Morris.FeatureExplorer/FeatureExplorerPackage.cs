using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Morris.FeatureExplorer
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[Guid(Consts.PackageGuidString)]
	[ProvideMenuResource("Menus.ctmenu", 2)]
	[ProvideToolWindow(typeof(FeatureExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.SolutionExplorer)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class FeatureExplorerPackage : AsyncPackage
	{
		internal static FeatureExplorerViewModel ViewModel { get; private set; }

		private SolutionEventsListener SolutionListener;
		private ProjectDocumentsListener DocumentsListener;

		protected override void Dispose(bool disposing)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (disposing)
			{
				DocumentsListener?.Dispose();
				SolutionListener?.Dispose();
				ViewModel?.ClearDte();
			}
			base.Dispose(disposing);
		}

		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);

			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			ViewModel = new FeatureExplorerViewModel();

			var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as DTE2;
			if (dte != null)
				ViewModel.SetDte(dte);

			var solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
			if (solution != null)
				SolutionListener = new SolutionEventsListener(solution, ViewModel);

			var tracker = await GetServiceAsync(typeof(SVsTrackProjectDocuments)) as IVsTrackProjectDocuments2;
			if (tracker != null)
				DocumentsListener = new ProjectDocumentsListener(tracker, ViewModel);

			await ShowFeatureExplorerCommand.InitializeAsync(this);
		}
	}
}
