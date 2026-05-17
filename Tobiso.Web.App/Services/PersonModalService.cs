namespace Tobiso.Web.App.Services;

public class PersonModalService
{
    // Support string-based person identifier (name) since persons are AI-generated on demand
    public static event Action<string>? OnGlobalPersonRequested;
    public event Action<string>? OnPersonRequested;

    public void RequestPerson(string personName)
    {
        OnPersonRequested?.Invoke(personName);
        OnGlobalPersonRequested?.Invoke(personName);
    }

    public static void TriggerGlobalRequest(string personName) => OnGlobalPersonRequested?.Invoke(personName);
}
