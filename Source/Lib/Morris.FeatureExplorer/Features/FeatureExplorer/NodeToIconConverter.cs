using Microsoft.VisualStudio.Shell;
using Morris.FeatureExplorer.Features.FeatureExplorer.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Morris.FeatureExplorer.Features.FeatureExplorer;

public sealed class NodeToIconConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length == 0 || values[0] is not NodeBase node)
			return null;

		// Ensure icon creation happens on UI thread and avoid deadlocks
		BitmapSource icon = ThreadHelper.JoinableTaskFactory.Run(async () => await NodeImageProvider.GetIconForNodeAsync(node));
		return icon;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
