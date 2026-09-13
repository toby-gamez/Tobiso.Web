// Renders KaTeX math inside a container. Dynamically loads KaTeX (CSS + JS) on
// first use, then re-renders on later calls once scripts are already present.
// Exposed as a named function and invoked from MarkdownContent via Blazor JS
// interop -- never via eval(), which Content-Security-Policy blocks.
let katexLoading = false;
let katexLoadCallbacks = [];

window.renderMarkdownMath = (containerId) => {
    const render = () => {
        try {
            const container = containerId ? document.getElementById(containerId) : null;
            if (container && window.renderMathInElement) {
                renderMathInElement(container, {
                    delimiters: [
                        { left: '$$', right: '$$', display: true },
                        { left: '$', right: '$', display: false },
                    ],
                });
            }
        } catch (e) { /* optional enhancement only */ }
        try {
            document.querySelectorAll('[data-math]').forEach((el) => {
                try {
                    const math = el.getAttribute('data-math');
                    if (math && window.katex && typeof window.katex.render === 'function') {
                        window.katex.render(math, el, { throwOnError: false, displayMode: false });
                    }
                } catch (_) { /* per-element failures must not abort the loop */ }
            });
        } catch (_) { /* ignore */ }
    };

    if (window.__katexLoaded) {
        render();
        return;
    }
    if (katexLoading) {
        katexLoadCallbacks.push(render);
        return;
    }
    katexLoading = true;
    try {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = 'https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css';
        document.head.appendChild(link);

        const s1 = document.createElement('script');
        s1.src = 'https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js';
        s1.defer = true;
        document.head.appendChild(s1);

        const s2 = document.createElement('script');
        s2.src = 'https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/contrib/auto-render.min.js';
        s2.defer = true;
        s2.onload = () => {
            window.__katexLoaded = true;
            katexLoading = false;
            render();
            katexLoadCallbacks.forEach((cb) => cb());
            katexLoadCallbacks = [];
        };
        s2.onerror = () => {
            window.__katexLoaded = true;
            katexLoading = false;
            katexLoadCallbacks = [];
        };
        document.head.appendChild(s2);
    } catch (e) {
        window.__katexLoaded = true;
        katexLoading = false;
        katexLoadCallbacks = [];
    }
};