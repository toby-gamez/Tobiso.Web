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

window.registerSearchShortcut = (dotNetRef) => {
    window.unregisterSearchShortcut();
    searchShortcutHandler = (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('ToggleFromJs');
        } else if (e.key === 'Escape') {
            // Document-level so it closes even when the search input never got focus.
            dotNetRef.invokeMethodAsync('CloseFromJs');
        }
    };
    document.addEventListener('keydown', searchShortcutHandler);
};

window.unregisterSearchShortcut = () => {
    if (searchShortcutHandler) {
        document.removeEventListener('keydown', searchShortcutHandler);
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

        const rect = article.getBoundingClientRect();
        const articleTop = rect.top + window.scrollY;
        const articleHeight = article.scrollHeight || 1;
        const viewportBottom = window.scrollY + window.innerHeight;
        const percent = Math.max(0, Math.min(100, Math.round(((viewportBottom - articleTop) / articleHeight) * 100)));

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
