using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Infrastructure.Services;

public class BrowserService : IBrowserService
{
    public async Task OpenAsync(Uri uri)
    {
        await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(uri);
    }
}
