using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace Morris.FeatureExplorer.Extensions;

internal static class AsyncPackageGetServiceExtension
{
	public static async ValueTask<T> GetServiceAsync<T>(this AsyncPackage package) where T : class
	{
		object service = await package.GetServiceAsync(typeof(T));
		return service as T;
	}
}
