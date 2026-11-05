namespace Tobiso.Web.App.Services;

/// <summary>
/// Lets any component reopen the cookie banner ("Nastavení cookies"), so a visitor can withdraw or
/// give consent as easily as they first answered (GDPR Art. 7(3)). Scoped: one per circuit.
/// </summary>
public class CookieConsentService
{
    public event Action? OpenRequested;

    public void Open() => OpenRequested?.Invoke();
}
