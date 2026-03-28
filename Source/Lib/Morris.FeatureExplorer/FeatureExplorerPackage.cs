using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Morris.FeatureExplorer;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(FeatureExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
[Guid("d8c9b0a1-5e7f-4b2d-9a3c-1f6e8d4b7c2a")]
public sealed class FeatureExplorerPackage : AsyncPackage
{
	public static readonly Guid CommandSetGuid = new("e9d0c1b2-6f80-4c3e-ab4d-207f9e5c8d3b");
	public const int ShowFeatureExplorerCommandId = 0x0100;

	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
		if (commandService is not null)
		{
			var commandId = new CommandID(CommandSetGuid, ShowFeatureExplorerCommandId);
			var menuItem = new MenuCommand(ShowFeatureExplorer, commandId);
			commandService.AddCommand(menuItem);
		}
	}

	private void ShowFeatureExplorer(object sender, EventArgs e)
	{
		_ = JoinableTaskFactory.RunAsync(async () =>
		{
			await ShowToolWindowAsync(
				typeof(FeatureExplorerToolWindow),
				id: 0,
				create: true,
				cancellationToken: DisposalToken);
		});
	}
}
