using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

internal static class AsyncPackageGetServiceExtension
{
	public static async ValueTask<T> GetServiceAsync<T>(this AsyncPackage package)
		where T : class
	=>
		(T)(await package.GetServiceAsync(typeof(T)))!;
}
