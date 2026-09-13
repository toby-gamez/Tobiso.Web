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
