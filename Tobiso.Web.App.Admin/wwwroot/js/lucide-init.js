// Renders icon SVGs as children of the placeholder element instead of using lucide's
// default createIcons(), which replaces the placeholder element itself. Replacing
// the element leaves Blazor holding a stale node reference to something no longer
// in the DOM, which throws "removeChild, o.parentNode is null" on the next render.
// Appending as a child keeps the Blazor-owned host element intact.
//
// lucide.icons[Name] is a raw [tag, attrs, children] node array (no per-icon toSvg()
// method in this build); lucide.createElement() turns that into a real DOM element.
//
// The lucide <script> tag loads from a CDN in parallel with the Blazor circuit
// connecting. Pages that only call initLucide() once, on firstRender, can lose
// that race - window.lucide isn't defined yet, so this silently no-ops and the
// icons stay blank forever since nothing calls it again. Retry with backoff
// until lucide is actually available instead of relying on every caller to
// re-invoke it.
let lucideRetryTimer = null;

window.initLucide = () => {
    try {
        if (!window.lucide || !window.lucide.icons || !window.lucide.createElement) {
            if (!lucideRetryTimer) {
                lucideRetryTimer = setTimeout(() => {
                    lucideRetryTimer = null;
                    window.initLucide();
                }, 100);
            }
            return;
        }
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

// Blazor applies a <video> element's src via a plain setAttribute during DOM
// diffing, which does not reliably kick off the browser's resource-selection
// algorithm the way parsing real HTML (e.g. direct navigation) does - the
// element can be left showing "no supported source" even though the URL is
// fine. Setting src as a real property (not just an attribute) and calling
// load() explicitly, the same imperative-fixup pattern as initLucide() above,
// works reliably.
window.initVideoSources = () => {
    document.querySelectorAll('video[data-src]').forEach((el) => {
        const src = el.getAttribute('data-src');
        if (!src || el.getAttribute('data-src-applied') === src) return;
        el.src = src;
        el.load();
        el.setAttribute('data-src-applied', src);
    });
};

window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('tobiso-admin-theme', theme);
};

window.getTheme = () => {
    return localStorage.getItem('tobiso-admin-theme') || 'system';
};

// Apply saved theme before Blazor's circuit connects, so the page doesn't
// flash the OS-preference theme first and then jump to the saved choice.
(function () {
    const saved = localStorage.getItem('tobiso-admin-theme');
    if (saved && saved !== 'system') {
        document.documentElement.setAttribute('data-theme', saved);
    }
})();
