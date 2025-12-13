using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Morris.FeatureExplorer;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideToolWindow(typeof(FeatureExplorerToolWindow))]
[ProvideMenuResource("Menus.ctmenu", 1)]
public sealed class FeatureExplorerPackage : AsyncPackage
{
	public const string PackageGuidString = "6a8e784c-96b4-4717-a560-99d2cbd83242";

	protected override async Task InitializeAsync(CancellationToken ct, IProgress<ServiceProgressData> progress)
	{
		await JoinableTaskFactory.SwitchToMainThreadAsync(ct);

		await FeatureExplorerViewModel.CreateAsync(this);
		await ShowFeatureExplorerCommand.InitializeAsync(this);
	}
}


