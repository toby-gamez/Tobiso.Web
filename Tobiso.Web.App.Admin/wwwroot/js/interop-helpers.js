// CSP-safe interop helpers for Blazor JS calls – never use eval().
// Called via JSRuntime.InvokeVoidAsync("functionName", elementRef, ...).
window.clickElement = (el) => el.click();

// Sign-in/sign-out go through the browser so the server can set or clear the HttpOnly session
// cookie; script never sees the token.
window.tobisoAuthPost = async function (url, body) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body ?? {})
        });
        let data = null;
        try { data = await response.json(); } catch { }
        return { status: response.status, message: data?.message ?? null };
    } catch {
        return { status: 0, message: null };
    }
};

// Older versions kept the JWT (and before that the password) in localStorage - remove them.
window.tobisoClearLegacyToken = function () {
    try {
        localStorage.removeItem('tobiso_jwt');
        localStorage.removeItem('blinked_username');
        localStorage.removeItem('blinked_password');
    } catch { }
};
