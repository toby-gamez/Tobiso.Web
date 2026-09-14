// Click-to-enlarge lightbox for article images. Reads caption/source from the
// data-caption/data-source attributes MarkdownContent.razor's PreprocessImageGroups
// sets on <img>, so the enlarged view shows the same info as the inline figcaption.
export function initImageLightbox(containerId) {
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
}

function openLightbox(src, alt, caption, source) {
    closeLightbox();

    const overlay = document.createElement('div');
    overlay.className = 'img-lightbox-overlay';

    const closeBtn = document.createElement('span');
    closeBtn.className = 'img-lightbox-close';
    closeBtn.innerHTML = '&times;';
    closeBtn.title = 'Zavřít';

    const wrapper = document.createElement('div');
    wrapper.className = 'img-lightbox-wrapper';

    const imgEl = document.createElement('img');
    imgEl.src = src;
    imgEl.alt = alt || '';
    wrapper.appendChild(imgEl);

    const captionPlain = stripMarkdown(caption);
    const sourcePlain = stripMarkdown(source);

    if (captionPlain || sourcePlain) {
        const meta = document.createElement('div');
        meta.className = 'img-lightbox-meta';

        if (captionPlain) {
            const cap = document.createElement('span');
            cap.className = 'img-lightbox-caption';
            cap.textContent = captionPlain;
            meta.appendChild(cap);
        }

        if (sourcePlain) {
            const srcEl = document.createElement('span');
            srcEl.className = 'img-lightbox-source';
            srcEl.textContent = sourcePlain;
            meta.appendChild(srcEl);
        }

        wrapper.appendChild(meta);
    }

    overlay.appendChild(closeBtn);
    overlay.appendChild(wrapper);
    document.body.appendChild(overlay);

    overlay.addEventListener('click', e => {
        if (e.target === overlay || e.target === closeBtn || e.target === imgEl) closeLightbox();
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
