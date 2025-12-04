window.pdfUtils = (function () {
    // Load html2pdf from CDN if it's not already loaded
    async function ensureHtml2Pdf() {
        if (window.html2pdf) return;
        return new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.9.3/html2pdf.bundle.min.js';
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Failed to load html2pdf.js'));
            document.head.appendChild(s);
        });
    }

    function sanitizeFileName(name) {
        return name.replace(/[^a-z0-9\-\_\u00C0-\u017F ]+/ig, '').replace(/\s+/g, '_').trim();
    }

    async function generatePdf(selector, filename) {
        try {
            await ensureHtml2Pdf();
            const el = document.querySelector(selector);
            if (!el) {
                console.warn('pdfUtils: selector not found', selector);
                return;
            }

            // Options tuned for document-like output (A4, margins in mm)
            const opt = {
                margin: 10, // mm
                filename: filename || 'document.pdf',
                image: { type: 'jpeg', quality: 0.95 },
                html2canvas: { scale: 2, useCORS: true, logging: false, allowTaint: false },
                jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
            };

            // Clone element to avoid side-effects
            const clone = el.cloneNode(true);

            // Remove interactive and navigational elements to make it look like a plain document
            const removeSelectors = [
                'button', '.btn', '.readmore', '.navbar', '.nav', '.nav-link', '#MyNavBar', '.mobile-menu', '.mobile-header', '.search', '.hero-actions', '.version-info', '.cookie-consent', '.loading-bar-container', '.loading-bar', '.navImg', '.nav-link-content'
            ];
            removeSelectors.forEach(sel => {
                clone.querySelectorAll(sel).forEach(n => n.remove());
            });

            // Remove decorative ribbons/svgs from clone
            clone.querySelectorAll('.decorative-ribbons, svg.ribbon, .ribbon').forEach(n => n.remove());

            // Remove style/script/link/meta/template elements entirely so CSS/text doesn't get copied into the PDF
            clone.querySelectorAll('style, script, link[rel="stylesheet"], noscript, meta, template').forEach(n => n.remove());

            // Create a wrapper (no styles injected) — user requested plain document without page/web styles
            const wrapper = document.createElement('div');

            // Create a plain-document version: strip all attributes and styles, keep only semantic tags
            function createPlainNode(node) {
                if (node.nodeType === Node.TEXT_NODE) {
                    return document.createTextNode(node.textContent);
                }

                if (node.nodeType !== Node.ELEMENT_NODE) return document.createDocumentFragment();

                const tag = node.tagName.toLowerCase();
                const allowed = new Set(['h1','h2','h3','h4','h5','h6','p','ul','ol','li','table','thead','tbody','tr','th','td','img','pre','code','blockquote','strong','b','em','i','a']);
                const skipTags = new Set(['style','script','link','noscript','meta','template']);

                // Skip these tags entirely (do not flatten their contents)
                if (skipTags.has(tag)) {
                    return document.createDocumentFragment();
                }

                // If this element is not allowed, but has children, flatten children into fragment
                if (!allowed.has(tag)) {
                    // special handling for links: convert to text with URL
                    if (tag === 'a') {
                        const frag = document.createDocumentFragment();
                        const text = node.textContent || '';
                        frag.appendChild(document.createTextNode(text));
                        const href = node.getAttribute('href');
                        if (href) frag.appendChild(document.createTextNode(' (' + href + ')'));
                        return frag;
                    }
                    const frag = document.createDocumentFragment();
                    node.childNodes.forEach(child => {
                        frag.appendChild(createPlainNode(child));
                    });
                    return frag;
                }

                // Create clean element without attributes
                const el = document.createElement(tag);

                // For images: keep src as absolute if possible and set alt text
                if (tag === 'img') {
                    const src = node.getAttribute('src') || node.getAttribute('data-src') || '';
                    if (src) {
                        // If relative, try to make absolute
                        let full = src;
                        if (!/^(https?:)?\/\//i.test(src)) {
                            full = src.startsWith('/') ? (location.origin + src) : (location.origin + '/' + src);
                        }
                        el.setAttribute('src', full);
                    }
                    const alt = node.getAttribute('alt') || '';
                    if (alt) el.setAttribute('alt', alt);
                    return el;
                }

                // Recursively copy allowed children
                node.childNodes.forEach(child => {
                    const childPlain = createPlainNode(child);
                    if (childPlain) el.appendChild(childPlain);
                });

                return el;
            }

            const plain = document.createElement('div');
            plain.className = 'pdf-root';
            // Build plain content from cloned node(s)
            clone.childNodes.forEach(n => {
                const pn = createPlainNode(n);
                if (pn) plain.appendChild(pn);
            });

            // Append plain content (no styles or fonts injected)
            wrapper.className = 'pdf-root';
            wrapper.appendChild(plain);

            // Use html2pdf to create PDF and trigger download
            window.html2pdf().set(opt).from(wrapper).save();
        } catch (err) {
            console.error('pdfUtils.generatePdf error', err);
        }
    }

    // Load jsPDF + autotable for text-based PDF generation
    async function ensureJsPdf() {
        if (window.jspdf && window.jspdf.jsPDF) return;
        return new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = 'https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js';
            s.onload = () => {
                // load autotable plugin
                const s2 = document.createElement('script');
                s2.src = 'https://cdnjs.cloudflare.com/ajax/libs/jspdf-autotable/3.5.28/jspdf.plugin.autotable.min.js';
                s2.onload = () => resolve();
                s2.onerror = () => reject(new Error('Failed to load jspdf-autotable'));
                document.head.appendChild(s2);
            };
            s.onerror = () => reject(new Error('Failed to load jspdf'));
            document.head.appendChild(s);
        });
    }

    // Generate a true-text PDF (not a screenshot). Keeps semantic headings, paragraphs and tables as selectable text.
    async function generateTextPdf(selector, filename) {
        try {
            await ensureJsPdf();
            const el = document.querySelector(selector);
            if (!el) return console.warn('pdfUtils.generateTextPdf: selector not found', selector);

            // Helper to produce a plain node (skip styles/scripts and keep semantic tags)
            function createPlainNode(node) {
                if (node.nodeType === Node.TEXT_NODE) {
                    return document.createTextNode(node.textContent);
                }
                if (node.nodeType !== Node.ELEMENT_NODE) return document.createDocumentFragment();
                const tag = node.tagName.toLowerCase();
                const allowed = new Set(['h1','h2','h3','h4','h5','h6','p','ul','ol','li','table','thead','tbody','tr','th','td','img','pre','code','blockquote','strong','b','em','i','a']);
                const skipTags = new Set(['style','script','link','noscript','meta','template']);

                // Skip these tags entirely (do not include their text content)
                if (skipTags.has(tag)) return document.createDocumentFragment();

                if (!allowed.has(tag)) {
                    if (tag === 'a') {
                        const frag = document.createDocumentFragment();
                        const text = node.textContent || '';
                        frag.appendChild(document.createTextNode(text));
                        const href = node.getAttribute('href');
                        if (href) frag.appendChild(document.createTextNode(' (' + href + ')'));
                        return frag;
                    }
                    const frag = document.createDocumentFragment();
                    node.childNodes.forEach(child => frag.appendChild(createPlainNode(child)));
                    return frag;
                }
                const el = document.createElement(tag);
                if (tag === 'img') {
                    // skip images for text PDF (user asked optionally; keep text only)
                    return document.createDocumentFragment();
                }
                node.childNodes.forEach(child => el.appendChild(createPlainNode(child)));
                return el;
            }

            // Build plain DOM
            const clone = el.cloneNode(true);
            // remove interactive elements
            ['button', '.btn', '.readmore', '.navbar', '.nav', '.nav-link', '#MyNavBar', '.mobile-menu', '.mobile-header', '.search', '.hero-actions', '.version-info', '.cookie-consent', '.loading-bar-container', '.loading-bar'].forEach(sel => {
                clone.querySelectorAll(sel).forEach(n => n.remove());
            });
            // Remove style/script/link/meta/template elements entirely so CSS/text doesn't get copied into the PDF
            clone.querySelectorAll('style, script, link[rel="stylesheet"], noscript, meta, template').forEach(n => n.remove());

            const plainRoot = document.createElement('div');
            clone.childNodes.forEach(n => {
                const pn = createPlainNode(n);
                if (pn) plainRoot.appendChild(pn);
            });

            // Setup jsPDF
            const { jsPDF } = window.jspdf;
            const doc = new jsPDF({ unit: 'mm', format: 'a4' });
            const leftMargin = 15;
            const rightMargin = 30; // increased right margin per request
            const pageWidth = 210;
            const usableWidth = pageWidth - leftMargin - rightMargin;
            let cursorY = leftMargin;
            const lineHeight = 6; // approx for 11pt

            // Normalize text: collapse multiple spaces and fix runs of single-letter spacing
            function normalizeText(s) {
                if (!s) return '';
                // Remove zero-width / BOM characters that sometimes appear between letters
                s = s.replace(/[\u200B\u200C\u200D\uFEFF]/g, '');
                // Normalize to NFC so composed characters (č,ě,ř,ů...) are single codepoints
                try { if (s.normalize) s = s.normalize('NFC'); } catch (e) { /* ignore */ }
                // collapse multiple whitespace to single space
                s = s.replace(/\s+/g, ' ').trim();

                // Helper: is token a single displayed letter (works with composed diacritics)
                function isSingleLetterToken(tok) {
                    if (!tok) return false;
                    // Count user-perceived characters by iterating codepoints
                    const chars = Array.from(tok);
                    if (chars.length !== 1) return false;
                    return /^\p{L}$/u.test(chars[0]);
                }

                // collapse runs of single-letter tokens (e.g. "P r a v i d l a") into words
                const tokens = s.split(' ');
                const out = [];
                for (let i = 0; i < tokens.length;) {
                    if (isSingleLetterToken(tokens[i])) {
                        // start of possible run
                        let run = [tokens[i]];
                        let j = i + 1;
                        while (j < tokens.length && isSingleLetterToken(tokens[j])) { run.push(tokens[j]); j++; }
                        if (run.length >= 3) {
                            out.push(run.join(''));
                        } else {
                            out.push(...run);
                        }
                        i = j;
                    } else {
                        out.push(tokens[i]);
                        i++;
                    }
                }
                return out.join(' ');
            }

            function addText(text, fontSize = 11) {
                const norm = normalizeText(text);
                doc.setFontSize(fontSize);
                // Always use normal font weight/style to avoid carrying web styles
                try { doc.setFont(undefined, 'normal'); } catch (e) { /* ignore if unsupported */ }
                const split = doc.splitTextToSize(norm, usableWidth);
                doc.text(split, leftMargin, cursorY);
                cursorY += split.length * (fontSize * 0.3528 + 1);
                // page bottom roughly A4 height minus bottom margin
                if (cursorY > 297 - rightMargin) { doc.addPage(); cursorY = leftMargin; }
            }

            // Walk through plainRoot children and render
            for (const node of Array.from(plainRoot.childNodes)) {
                if (node.nodeType === Node.TEXT_NODE) {
                    const txt = (node.textContent || '').trim();
                    if (txt) addText(txt);
                    continue;
                }
                if (node.nodeType !== Node.ELEMENT_NODE) continue;
                const tag = node.tagName.toLowerCase();
                if (tag.startsWith('h')) {
                    const level = parseInt(tag.substring(1)) || 1;
                    const size = Math.max(14 - (level - 1) * 2, 10);
                    // Render headings as larger plain text (no bold/italic)
                    addText(node.textContent.trim(), size);
                    cursorY += 2;
                } else if (tag === 'p' || tag === 'blockquote') {
                    // Render paragraphs and blockquotes as plain text
                    addText(node.textContent.trim(), 11);
                    cursorY += 1;
                } else if (tag === 'ul' || tag === 'ol') {
                    const items = Array.from(node.querySelectorAll('li'));
                    items.forEach((li, idx) => {
                        const bullet = tag === 'ol' ? (idx + 1) + '. ' : '• ';
                        addText(bullet + ' ' + li.textContent.trim(), 11);
                    });
                    cursorY += 1;
                } else if (tag === 'pre' || tag === 'code') {
                    // Render code blocks as monospaced-like smaller plain text (but no web styles)
                    addText(node.textContent.trim(), 9);
                    cursorY += 1;
                } else if (tag === 'table') {
                    // build table data
                    const rows = [];
                    const header = [];
                    const ths = node.querySelectorAll('thead th');
                    if (ths.length) {
                        ths.forEach(th => header.push(th.textContent.trim()));
                    }
                    const trs = node.querySelectorAll('tbody tr');
                    trs.forEach(tr => {
                        const cols = Array.from(tr.querySelectorAll('td')).map(td => td.textContent.trim());
                        rows.push(cols);
                    });
                    // use autoTable
                    if (window.jspdf && doc.autoTable) {
                        const startY = cursorY + 2;
                        doc.autoTable({ head: header.length ? [header] : [], body: rows, startY, margin: { left: leftMargin, right: rightMargin }, theme: 'striped', styles: { fontSize: 10 } });
                        cursorY = doc.lastAutoTable ? (doc.lastAutoTable.finalY + 4) : (startY + 10);
                    } else {
                        // fallback: render simple text rows
                        rows.forEach(r => addText(r.join(' | '), 10));
                        cursorY += 2;
                    }
                } else {
                    const txt = node.textContent.trim();
                    if (txt) addText(txt, 11);
                }
            }

            doc.save(filename || 'document.pdf');
        } catch (err) {
            console.error('pdfUtils.generateTextPdf error', err);
        }
    }

    return {
        generatePdf: generatePdf,
        generateTextPdf: generateTextPdf,
        _sanitizeFileName: sanitizeFileName
    };
})();
