using Morris.FeatureExplorer.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Morris.FeatureExplorer;

internal class NodeToIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value switch
		{
			FolderNode => "\U0001F4C1",
			FileNode => "\U0001F4C4",
			_ => "\u2753"
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
