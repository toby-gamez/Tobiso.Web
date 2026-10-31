// Click-to-enlarge lightbox for article images and graphs. Reads caption/source from the
// data-caption/data-source attributes MarkdownContent.razor's PreprocessImageGroups
// sets on <img>, so the enlarged view shows the same info as the inline figcaption.
export function initImageLightbox(containerId) {
    bindVennOps();
    const container = document.getElementById(containerId);
    if (!container) return;

    container.querySelectorAll('img').forEach(img => {
        if (img.getAttribute('data-lightbox-bound')) return;
        img.setAttribute('data-lightbox-bound', '1');
        img.addEventListener('click', e => {
            e.stopPropagation();
            openLightbox(img.src, img.alt, img.dataset.caption || '', img.dataset.source || '');
        });
    });

    // Graphs ([graf1] blocks, rendered to inline SVG by GraphRenderer.cs) enlarge the same way.
    // Flowcharts ([vyvoj1], FlowchartRenderer.cs) and Venn diagrams ([venn1], VennRenderer.cs) too.
    container.querySelectorAll('.post-graph, .post-flowchart, .post-venn').forEach(graph => {
        if (graph.getAttribute('data-lightbox-bound')) return;
        graph.setAttribute('data-lightbox-bound', '1');
        graph.addEventListener('click', e => {
            if (e.target.closest('a, .venn-ops')) return;
            e.stopPropagation();
            openGraphLightbox(graph);
        });
    });
}

// Venn diagram terms (průnik, sjednocení, rozdíl, doplněk; VennRenderer.cs): choosing one lights up
// its regions and shows the definition. One delegated listener, so it also works in the lightbox
// copy and for diagrams that appear after a re-render.
function bindVennOps() {
    if (window.__vennOpsBound) return;
    window.__vennOpsBound = true;
    document.addEventListener('click', e => {
        const btn = e.target.closest('.venn-op');
        if (!btn) return;
        const figure = btn.closest('.post-venn');
        if (!figure) return;
        e.stopPropagation();

        const wasOn = btn.getAttribute('aria-pressed') === 'true';
        figure.querySelectorAll('.venn-op').forEach(b => b.setAttribute('aria-pressed', 'false'));
        figure.querySelectorAll('.venn-region, .venn-text').forEach(r => r.classList.remove('venn-hl'));
        const svg = figure.querySelector('.venn-svg');
        if (svg) svg.classList.remove('venn-active');
        const desc = figure.querySelector('.venn-op-desc');

        if (wasOn) {
            if (desc) desc.textContent = 'Klepni na pojem a ukáže se v diagramu.';
            return;
        }
        btn.setAttribute('aria-pressed', 'true');
        const masks = new Set((btn.dataset.masks || '').split(',').filter(Boolean));
        figure.querySelectorAll('.venn-region, .venn-text').forEach(r => {
            if (masks.has(r.getAttribute('data-mask'))) r.classList.add('venn-hl');
        });
        if (svg) svg.classList.add('venn-active');
        if (desc) desc.textContent = btn.dataset.desc || '';
    });
}

function openGraphLightbox(graph) {
    // A clone keeps title, legend and notes (incl. already-rendered KaTeX). Its clipPath ids
    // duplicate the original's, which is harmless: url(#id) resolves to identical geometry.
    const clone = graph.cloneNode(true);
    clone.classList.add('post-graph--lightbox');
    clone.removeAttribute('data-lightbox-bound');

    const wrapper = document.createElement('div');
    wrapper.className = 'img-lightbox-wrapper';

    const frame = document.createElement('div');
    frame.className = 'img-lightbox-frame img-lightbox-frame--graph';
    frame.appendChild(clone);
    wrapper.appendChild(frame);

    // A click anywhere on the card closes it, except on the Venn term buttons (and links).
    showOverlay(wrapper, target => frame.contains(target) && !target.closest('.venn-ops, a'));
}

function openLightbox(src, alt, caption, source) {
    const wrapper = document.createElement('div');
    wrapper.className = 'img-lightbox-wrapper';

    // The rounded corners + shadow live on this frame (not on the <img> itself) because
    // some mobile browsers don't reliably clip a replaced element (img/object-fit) to its
    // own border-radius, which showed up as square bottom corners on mobile.
    const frame = document.createElement('div');
    frame.className = 'img-lightbox-frame';

    const imgEl = document.createElement('img');
    imgEl.src = src;
    imgEl.alt = alt || '';
    frame.appendChild(imgEl);
    wrapper.appendChild(frame);

    const captionPlain = stripMarkdown(caption);
    const sourcePlain = stripMarkdown(source);

    if (captionPlain || sourcePlain) {
        const meta = document.createElement('div');
        meta.className = 'img-lightbox-meta';

        if (captionPlain) {
            const cap = document.createElement('span');
            cap.className = 'img-lightbox-caption';
            appendTextWithLinks(cap, captionPlain);
            meta.appendChild(cap);
        }

        if (sourcePlain) {
            const srcEl = document.createElement('span');
            srcEl.className = 'img-lightbox-source';
            appendTextWithLinks(srcEl, sourcePlain);
            meta.appendChild(srcEl);
        }

        wrapper.appendChild(meta);
    }

    showOverlay(wrapper, target => target === imgEl || target === frame);
}

// Shows wrapper in the full-screen overlay. Clicking the backdrop, the close button,
// or anything closesOnClick accepts closes it, as does Escape.
function showOverlay(wrapper, closesOnClick) {
    closeLightbox();

    const overlay = document.createElement('div');
    overlay.className = 'img-lightbox-overlay';

    const closeBtn = document.createElement('span');
    closeBtn.className = 'img-lightbox-close';
    closeBtn.innerHTML = '&times;';
    closeBtn.title = 'Zavřít';

    overlay.appendChild(closeBtn);
    overlay.appendChild(wrapper);
    document.body.appendChild(overlay);

    overlay.addEventListener('click', e => {
        if (e.target === overlay || e.target === closeBtn || closesOnClick(e.target)) closeLightbox();
    });
    closeBtn.addEventListener('click', () => closeLightbox());

    window.__lightboxEscHandler = e => {
        if (e.key === 'Escape') closeLightbox();
    };
    document.addEventListener('keydown', window.__lightboxEscHandler);
}

function closeLightbox() {
    const existing = document.querySelector('.img-lightbox-overlay');
    if (existing) existing.remove();
    if (window.__lightboxEscHandler) {
        document.removeEventListener('keydown', window.__lightboxEscHandler);
        delete window.__lightboxEscHandler;
    }
}

// Appends text to el, turning any bare http(s):// URL into a real, clickable link
// (e.g. "Autor: ... https://commons.wikimedia.org/..."). Built with DOM nodes rather
// than innerHTML since the source text isn't pre-sanitized.
function appendTextWithLinks(el, text) {
    const urlRegex = /(https?:\/\/[^\s]+)/g;
    let lastIndex = 0;
    let match;
    while ((match = urlRegex.exec(text)) !== null) {
        if (match.index > lastIndex) {
            el.appendChild(document.createTextNode(text.slice(lastIndex, match.index)));
        }

        let url = match[0];
        // Trailing punctuation is usually sentence punctuation, not part of the URL.
        const trailingMatch = url.match(/[.,;:!?)\]}'"]+$/);
        let trailing = '';
        if (trailingMatch) {
            trailing = trailingMatch[0];
            url = url.slice(0, url.length - trailing.length);
        }

        const a = document.createElement('a');
        a.href = url;
        a.target = '_blank';
        a.rel = 'noopener noreferrer';
        a.textContent = url;
        el.appendChild(a);
        if (trailing) el.appendChild(document.createTextNode(trailing));

        lastIndex = urlRegex.lastIndex;
    }

    if (lastIndex < text.length) {
        el.appendChild(document.createTextNode(text.slice(lastIndex)));
    }
}

// Strip markdown syntax down to plain text for the lightbox's caption/source labels.
function stripMarkdown(text) {
    if (!text) return '';
    return text
        .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
        .replace(/!\[([^\]]*?)\]\([^)]*\)/g, '$1')
        .replace(/\*{1,2}([^*]+)\*{1,2}/g, '$1')
        .replace(/_{1,2}([^_]+)_{1,2}/g, '$1')
        .replace(/`([^`]+)`/g, '$1')
        .trim();
}
