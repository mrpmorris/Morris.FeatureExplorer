using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

internal class NodeToIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			FolderNode => "📁",
			FileNode => "📄",
			_ => "❓"
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
