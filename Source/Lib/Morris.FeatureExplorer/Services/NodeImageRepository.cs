using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class NodeImageProvider
{
	public static async Task<BitmapSource> GetIconForNodeAsync(NodeBase node)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var imageService = (await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsImageService))) as IVsImageService2;

		ImageMoniker moniker = node switch {
			FolderNode folder => folder.IsExpanded ? KnownMonikers.FolderBottomPanel : KnownMonikers.FolderClosed,
			FileNode => imageService.GetImageMonikerForFile("dummy" + NormalizeExtension(Path.GetExtension(node.Name))),
			_ => KnownMonikers.Document
		};

		if (moniker.Id == 0) moniker = KnownMonikers.Document;

		var imageAttributes =
			new ImageAttributes {
				Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
				ImageType = (uint)_UIImageType.IT_Bitmap,
				Format = (uint)_UIDataFormat.DF_WPF,
				Dpi = 96,
				LogicalHeight = 16,
				LogicalWidth = 16,
				StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
			};

		object imageObject = ImageLibrary.Default.GetImage(moniker, imageAttributes);

		if (imageObject is BitmapSource bmp) return bmp;
		if (imageObject is ImageSource src) return ToBitmapSource(src);

		return BitmapSource.Create(1, 1, 96, 96, PixelFormats.Pbgra32, null, new byte[4], 4);
	}

	private static string NormalizeExtension(string ext)
		=> string.IsNullOrWhiteSpace(ext) ? ".txt" : (ext.StartsWith(".") ? ext : "." + ext);

	private static BitmapSource ToBitmapSource(ImageSource src)
	{
		var dv = new System.Windows.Media.DrawingVisual();
		using (var dc = dv.RenderOpen())
			dc.DrawImage(src, new System.Windows.Rect(0, 0, 16, 16)); // render at a small default; UI can scale
		var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
		rtb.Render(dv);
		return rtb;
	}
}
