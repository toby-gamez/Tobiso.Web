// Renders icon SVGs as children of the placeholder element instead of using lucide's
// default createIcons(), which replaces the placeholder element itself. Replacing
// the element leaves Blazor holding a stale node reference to something no longer
// in the DOM, which throws "removeChild, o.parentNode is null" on the next render.
// Appending as a child keeps the Blazor-owned host element intact.
//
// lucide.icons[Name] is a raw [tag, attrs, children] node array (no per-icon toSvg()
// method in this build); lucide.createElement() turns that into a real DOM element.
window.initLucide = () => {
    try {
        if (!window.lucide || !window.lucide.icons || !window.lucide.createElement) return;
        document.querySelectorAll('[data-lucide]').forEach((el) => {
            const name = el.getAttribute('data-lucide');
            if (!name || el.getAttribute('data-lucide-rendered') === name) return;
            const pascalName = name.split('-').map(p => p.charAt(0).toUpperCase() + p.slice(1)).join('');
            const iconNode = window.lucide.icons[pascalName];
            if (!iconNode) return;
            const [tag, attrs, children] = iconNode;
            const svg = window.lucide.createElement([tag, { ...attrs, 'stroke-width': 2.75, class: `lucide lucide-${name}` }, children]);
            el.replaceChildren(svg);
            el.setAttribute('data-lucide-rendered', name);
        });
    } catch (e) {
        // Icons may not be loaded yet; safe to ignore
    }
};

window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('tobiso-theme', theme);
};

window.getTheme = () => {
    return localStorage.getItem('tobiso-theme') || 'system';
};

let searchShortcutHandler = null;

const isEditableElement = (el) =>
    !!el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.isContentEditable);

window.registerSearchShortcut = (dotNetRef) => {
    window.unregisterSearchShortcut();
    searchShortcutHandler = (e) => {
        // e.code is layout-independent (physical key), e.key can differ across
        // keyboard layouts/browsers — check both so Ctrl+K is caught reliably.
        const isK = e.key?.toLowerCase() === 'k' || e.code === 'KeyK';
        if ((e.ctrlKey || e.metaKey) && isK) {
            // Best-effort: some browsers (notably Firefox) treat Ctrl/Cmd+K as a
            // reserved shortcut that page JS cannot override, so it may not fire.
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('ToggleFromJs');
        } else if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey && !isEditableElement(document.activeElement)) {
            // Guaranteed-to-work fallback (no browser reserves a bare "/"), same
            // convention GitHub/docs sites use.
            e.preventDefault();
            dotNetRef.invokeMethodAsync('ToggleFromJs');
        } else if (e.key === 'Escape') {
            // Document-level so it closes even when the search input never got focus.
            dotNetRef.invokeMethodAsync('CloseFromJs');
        }
    };
    // Capture phase + non-passive so preventDefault reliably beats the browser's
    // own reserved Ctrl+K/Cmd+K (focus address bar) handling in some browsers.
    document.addEventListener('keydown', searchShortcutHandler, { capture: true, passive: false });
};

window.unregisterSearchShortcut = () => {
    if (searchShortcutHandler) {
        document.removeEventListener('keydown', searchShortcutHandler, { capture: true });
        searchShortcutHandler = null;
    }
};

window.scrollToId = (id) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
};

// Tracks scroll position over an article: reports which h2 is currently under the
// sticky header and how far through the article the viewport has scrolled.
let readingScrollHandler = null;
let readingRaf = null;

window.initReadingProgress = (dotNetRef, articleSelector) => {
    window.disposeReadingProgress();

    const compute = () => {
        readingRaf = null;
        const article = document.querySelector(articleSelector);
        if (!article) return;

        // Progress = how far scrolled past the article's top, normalized to the
        // range that actually needs scrolling. Using viewport-overlap instead (how
        // much of the article the viewport already covers at rest) gave a nonzero
        // reading — e.g. 28% — the instant the page loaded, before any scrolling.
        const rect = article.getBoundingClientRect();
        const articleTop = rect.top + window.scrollY;
        const articleHeight = article.scrollHeight || 1;
        const scrollableRange = Math.max(1, articleHeight - window.innerHeight);
        const scrolled = window.scrollY - articleTop;
        const percent = Math.max(0, Math.min(100, Math.round((scrolled / scrollableRange) * 100)));

        let activeId = null;
        const probe = window.scrollY + 140;
        article.querySelectorAll('h2[id]').forEach((h) => {
            if (h.offsetTop <= probe) activeId = h.id;
        });
        const headings = article.querySelectorAll('h2[id]');
        if (!activeId && headings.length > 0) activeId = headings[0].id;

        dotNetRef.invokeMethodAsync('OnReadingProgress', activeId, percent);
    };

    readingScrollHandler = () => {
        if (readingRaf) return;
        readingRaf = requestAnimationFrame(compute);
    };

    window.addEventListener('scroll', readingScrollHandler, { passive: true });
    window.addEventListener('resize', readingScrollHandler);
    compute();
};

window.disposeReadingProgress = () => {
    if (readingScrollHandler) {
        window.removeEventListener('scroll', readingScrollHandler);
        window.removeEventListener('resize', readingScrollHandler);
        readingScrollHandler = null;
    }
    if (readingRaf) {
        cancelAnimationFrame(readingRaf);
        readingRaf = null;
    }
};

// Apply saved theme on load
(function () {
    const saved = localStorage.getItem('tobiso-theme');
    if (saved && saved !== 'system') {
        document.documentElement.setAttribute('data-theme', saved);
    }
})();

// Cookie consent (Google Analytics). The default-denied Consent Mode signal is
// set inline in App.razor's <head>, before gtag.js loads; these just persist
// the visitor's choice and flip the signal to granted/denied afterward.
window.getCookieConsent = () => {
    try {
        return localStorage.getItem('tobiso-cookie-consent');
    } catch (e) {
        return null;
    }
};

window.acceptCookieConsent = () => {
    try { localStorage.setItem('tobiso-cookie-consent', 'accepted'); } catch (e) { /* ignore */ }
    window.gtag?.('consent', 'update', { analytics_storage: 'granted' });
};

window.declineCookieConsent = () => {
    try { localStorage.setItem('tobiso-cookie-consent', 'declined'); } catch (e) { /* ignore */ }
    window.gtag?.('consent', 'update', { analytics_storage: 'denied' });
};
