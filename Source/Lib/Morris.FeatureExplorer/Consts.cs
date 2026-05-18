using System;

namespace Morris.FeatureExplorer
{
	internal static class Consts
	{
		public const string PackageGuidString = "99D4D800-6E98-4CCE-A79E-6289362828EA";
		public const string CommandSetGuidString = "A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D";
		public const int CreateFeatureCommandId = 0x0104;
		public const int CreateFolderCommandId = 0x0103;
		public const int DeleteFolderCommandId = 0x0102;
		public const int FeatureExplorerCommandId = 0x0100;
		public const int FolderContextMenuId = 0x1030;
		public const int RenameFolderCommandId = 0x0101;
		public const int TreeViewBackgroundContextMenuId = 0x1031;

		public static readonly Guid CommandSetGuid = new Guid(CommandSetGuidString);
	}
}
