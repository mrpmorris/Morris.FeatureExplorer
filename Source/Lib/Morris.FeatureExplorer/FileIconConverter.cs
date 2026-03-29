using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Morris.FeatureExplorer
{
	public sealed class FileIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string name = value as string;
			if (string.IsNullOrEmpty(name))
				return KnownMonikers.Document;

			string ext = Path.GetExtension(name);
			if (string.IsNullOrEmpty(ext))
				return KnownMonikers.Document;

			switch (ext.ToLowerInvariant())
			{
				case ".cs": return KnownMonikers.CSFileNode;
				case ".vb": return KnownMonikers.VBFileNode;
				case ".fs": return KnownMonikers.FSFileNode;
				case ".xaml": return KnownMonikers.WPFFile;
				case ".xml": return KnownMonikers.XMLFile;
				case ".json": return KnownMonikers.JSONScript;
				case ".js": return KnownMonikers.JSScript;
				case ".ts": return KnownMonikers.TSFileNode;
				case ".css": return KnownMonikers.StyleSheet;
				case ".html":
				case ".htm": return KnownMonikers.HTMLFile;
				case ".razor":
				case ".cshtml": return KnownMonikers.WebFile;
				case ".csproj": return KnownMonikers.CSProjectNode;
				case ".sln": return KnownMonikers.Solution;
				case ".config": return KnownMonikers.ConfigurationFile;
				case ".txt": return KnownMonikers.TextFile;
				case ".md": return KnownMonikers.MarkdownFile;
				case ".sql": return KnownMonikers.DatabaseScript;
				case ".png":
				case ".jpg":
				case ".jpeg":
				case ".gif":
				case ".bmp":
				case ".ico": return KnownMonikers.Image;
				default: return KnownMonikers.Document;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
