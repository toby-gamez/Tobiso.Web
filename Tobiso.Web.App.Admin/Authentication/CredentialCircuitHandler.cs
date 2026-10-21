using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Makes the circuit's own CredentialStore visible to AuthenticationHeaderHandler (which the
/// HttpClient factory resolves in a different DI scope) for the duration of each inbound
/// circuit activity - the pattern ASP.NET Core documents for reaching circuit-scoped services
/// from outside the circuit's scope.
/// </summary>
public class CredentialCircuitHandler(CredentialStore store) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next) =>
        context =>
        {
            CredentialStore.Current = store;
            return next(context);
        };
}
