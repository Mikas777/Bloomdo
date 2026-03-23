namespace Bloomdo.Client.Core.Interfaces;

public interface IBrowserService
{
    Task OpenAsync(Uri uri);
}
