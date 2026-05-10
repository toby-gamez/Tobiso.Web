namespace Tobiso.Web.App.Services;

public class PostsGraphModalService
{
    public event Action? OnGraphModalRequested;
    
    public void OpenGraphModal()
    {
        OnGraphModalRequested?.Invoke();
    }
}
