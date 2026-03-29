using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Features.FeatureExplorer;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer;

internal sealed class ShowFeatureExplorerCommand
{
	private static readonly Guid CommandSetGuid = new("e9d0c1b2-6f80-4c3e-ab4d-207f9e5c8d3b");
	private const int CommandId = 0x0100;

	private readonly AsyncPackage Package;

	public static async Task InitializeAsync(AsyncPackage package)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
		if (commandService is not null)
		{
			var instance = new ShowFeatureExplorerCommand(package);
			var menuCommandId = new CommandID(CommandSetGuid, CommandId);
			var menuItem = new MenuCommand(instance.Execute, menuCommandId);
			commandService.AddCommand(menuItem);
		}
	}

	private ShowFeatureExplorerCommand(AsyncPackage package)
	{
		Package = package;
	}

	private void Execute(object sender, EventArgs e)
	{
		_ = Package.JoinableTaskFactory.RunAsync(async () =>
		{
			await Package.ShowToolWindowAsync(
				typeof(FeatureExplorerToolWindow),
				id: 0,
				create: true,
				cancellationToken: Package.DisposalToken);
		});
	}
}
