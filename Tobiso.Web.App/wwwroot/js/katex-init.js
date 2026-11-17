// Renders KaTeX math inside a container. Loads the self-hosted KaTeX (wwwroot/lib/katex, 0.16.8:
// CSS, core, auto-render, mhchem) on first use, then re-renders on later calls once it is loaded.
// Exposed as a named function and invoked from MarkdownContent via Blazor JS
// interop -- never via eval(), which Content-Security-Policy blocks.
// Keep tools/*/ KaTeX versions in sync with lib/katex when upgrading.
const KATEX_BASE = 'lib/katex/';

let katexLoaded = false;
let katexReady = null;

// Fresh object per call: KaTeX mutates the macros map on \gdef. \R, \N and \Z are built in.
const katexMacros = () => ({
    '\\Q': '\\mathbb{Q}',
    '\\C': '\\mathbb{C}',
    '\\abs': '\\left|#1\\right|',
});

const loadScript = (src) => new Promise((resolve, reject) => {
    const s = document.createElement('script');
    s.src = src;
    s.onload = resolve;
    s.onerror = reject;
    document.head.appendChild(s);
});

// katex core first; auto-render and mhchem both need it. mhchem (\ce{H2O}) is optional.
const ensureKatex = () => {
    if (!katexReady) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = KATEX_BASE + 'katex.min.css';
        document.head.appendChild(link);

        katexReady = loadScript(KATEX_BASE + 'katex.min.js').then(() => Promise.all([
            loadScript(KATEX_BASE + 'contrib/auto-render.min.js'),
            loadScript(KATEX_BASE + 'contrib/mhchem.min.js').catch(() => { /* optional */ }),
        ]));
    }
    return katexReady;
};

const renderMath = (containerId) => {
    try {
        const container = containerId ? document.getElementById(containerId) : null;
        if (container && window.renderMathInElement) {
            renderMathInElement(container, {
                delimiters: [
                    { left: '$$', right: '$$', display: true },
                    { left: '$', right: '$', display: false },
                ],
                throwOnError: false,
                macros: katexMacros(),
            });
        }
    } catch (e) { /* optional enhancement only */ }
    try {
        document.querySelectorAll('[data-math]').forEach((el) => {
            try {
                const math = el.getAttribute('data-math');
                const display = el.getAttribute('data-display') === 'true';
                // Already rendered from this exact source: nothing to do.
                if (!math || (el.__mathSrc === math && el.firstChild)) return;
                if (window.katex && typeof window.katex.render === 'function') {
                    window.katex.render(math, el, { throwOnError: false, displayMode: display, macros: katexMacros() });
                    el.__mathSrc = math;
                }
            } catch (_) { /* per-element failures must not abort the loop */ }
        });
    } catch (_) { /* ignore */ }
};

window.renderMarkdownMath = (containerId) => {
    if (katexLoaded) {
        renderMath(containerId);
        return;
    }
    ensureKatex().then(() => {
        katexLoaded = true;
        renderMath(containerId);
    }, () => { /* KaTeX failed to load: leave the raw text in place */ });
};
