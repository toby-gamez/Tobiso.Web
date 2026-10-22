// Redeems the one-time HttpOnly cookie set by /api/auth/google-callback for the student JWT.
// Kept in a file (not eval'd) because the Content-Security-Policy forbids eval.
window.tobisoClaimGoogleToken = async function () {
    try {
        const response = await fetch('/api/auth/google-token', { method: 'POST', credentials: 'same-origin' });
        if (!response.ok) return null;
        const body = await response.json();
        return body.token || null;
    } catch {
        return null;
    }
};
