using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Morris.FeatureExplorer
{
	internal sealed class ShowFeatureExplorerCommand
	{
		private readonly FeatureExplorerPackage _package;

		private ShowFeatureExplorerCommand(FeatureExplorerPackage package, OleMenuCommandService commandService)
		{
			_package = package;

			var commandId = new CommandID(Consts.CommandSetGuid, Consts.FeatureExplorerCommandId);
			var menuCommand = new MenuCommand(Execute, commandId);
			commandService.AddCommand(menuCommand);
		}

		public static async Task InitializeAsync(FeatureExplorerPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService != null)
			{
				new ShowFeatureExplorerCommand(package, commandService);
			}
		}

		private void Execute(object sender, EventArgs e)
		{
			_ = ExecuteAsync();
		}

		private async Task ExecuteAsync()
		{
			ToolWindowPane window = await _package.ShowToolWindowAsync(
				typeof(FeatureExplorerToolWindow),
				0,
				create: true,
				_package.DisposalToken);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			if (window?.Frame is IVsWindowFrame frame)
				Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
		}
	}
}
