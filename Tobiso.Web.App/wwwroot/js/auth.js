// Sign-in, sign-out and password calls go through the browser (not the Blazor circuit) so the
// server can set or clear the HttpOnly session cookie. Script never sees the token itself.
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
        return { status: response.status, message: data?.message ?? null, code: data?.code ?? null };
    } catch {
        return { status: 0, message: null, code: null };
    }
};

// Tokens used to live in localStorage; remove any left behind by older versions.
window.tobisoClearLegacyToken = function () {
    try { localStorage.removeItem('tobiso_student_token'); } catch { }
};
