// Minimal wrapper so Blazor can call window.pdfUtils safely
(function(){
    window.pdfBlazorInterop = {
        generateTextPdf: function(selector, filename) {
            if (window.pdfUtils && typeof window.pdfUtils.generateTextPdf === 'function') {
                try { window.pdfUtils.generateTextPdf(selector, filename); }
                catch (e) { console.error('pdfBlazorInterop.generateTextPdf error', e); }
            } else {
                console.error('pdfUtils.generateTextPdf is not available');
            }
        },
        generatePdf: function(selector, options) {
            if (window.pdfUtils && typeof window.pdfUtils.generatePdf === 'function') {
                try { window.pdfUtils.generatePdf(selector, options); }
                catch (e) { console.error('pdfBlazorInterop.generatePdf error', e); }
            } else {
                console.error('pdfUtils.generatePdf is not available');
            }
        },
        sanitizeFileName: function(name) {
            if (window.pdfUtils && typeof window.pdfUtils._sanitizeFileName === 'function') {
                try { return window.pdfUtils._sanitizeFileName(name); }
                catch (e) { console.error('pdfBlazorInterop.sanitizeFileName error', e); }
            }
            // fallback sanitizer: remove problematic chars and replace spaces
            if (!name) return '';
            return name.replace(/[^a-z0-9\-\_\u00C0-\u017F ]+/ig, '').replace(/\s+/g,'_').trim();
        }
    };
})();
