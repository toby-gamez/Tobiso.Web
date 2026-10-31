namespace Tobiso.Web.App.Services;

public class AccountRequiredModalService
{
    public event Action<string?>? OnShowRequested;

    public void Show(string? message = null) => OnShowRequested?.Invoke(message);
}
