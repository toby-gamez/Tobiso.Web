namespace Tobiso.Web.App.Services;

public class AddendumModalService
{
    public static event Action<int>? OnGlobalAddendumRequested;
    public event Action<int>? OnAddendumRequested;
    
    public void RequestAddendum(int addendumId)
    {
        OnAddendumRequested?.Invoke(addendumId);
        OnGlobalAddendumRequested?.Invoke(addendumId);
    }
    
    public static void TriggerGlobalRequest(int addendumId)
    {
        OnGlobalAddendumRequested?.Invoke(addendumId);
    }
}
