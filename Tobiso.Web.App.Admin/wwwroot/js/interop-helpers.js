// CSP-safe interop helpers for Blazor JS calls – never use eval().
// Called via JSRuntime.InvokeVoidAsync("functionName", elementRef, ...).
window.clickElement = (el) => el.click();
