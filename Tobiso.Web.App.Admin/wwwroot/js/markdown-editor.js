// markdown-editor.js - JavaScript modul pro Markdown editor
let editorInstances = new Map();

export function initializeMarkdownEditor(textareaId, dotNetRef, placeholder, initialValue) {
    // Počkáme, až se DOM načte
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            createEditor(textareaId, dotNetRef, placeholder, initialValue);
        });
    } else {
        createEditor(textareaId, dotNetRef, placeholder, initialValue);
    }
}

function createEditor(textareaId, dotNetRef, placeholder, initialValue) {
    const textarea = document.getElementById(textareaId);
    if (!textarea) {
        console.error(`Textarea s ID ${textareaId} nebyla nalezena`);
        return;
    }

    // Pokud editor už existuje, znič ho
    if (editorInstances.has(textareaId)) {
        const oldEditor = editorInstances.get(textareaId);
        oldEditor.toTextArea();
        editorInstances.delete(textareaId);
    }

    try {
        const easyMDE = new EasyMDE({
            element: textarea,
            placeholder: placeholder || 'Začněte psát váš Markdown obsah...',
            spellChecker: false,
            autofocus: false,
            // We load Font Awesome ourselves (App.razor) - without this, EasyMDE always injects
            // its own <link> to the long-dead maxcdn.bootstrapcdn.com/font-awesome, since its
            // "already loaded" detection only recognizes stylesheets from that same dead host.
            autoDownloadFontAwesome: false,
            autosave: {
                enabled: false
            },
            status: ['autosave', 'lines', 'words', 'cursor'],
            toolbar: [
                'bold', 'italic', 'heading', '|',
                'quote', 'unordered-list', 'ordered-list', '|',
                'link', 'image', '|',
                'code', 'table', '|',
                'preview', 'side-by-side', 'fullscreen', '|',
                'guide'
            ],
            initialValue: initialValue || '',
            renderingConfig: {
                singleLineBreaks: false,
                codeSyntaxHighlighting: true,
            },
            shortcuts: {
                drawTable: "Cmd-Alt-T",
                toggleBold: "Cmd-B",
                toggleItalic: "Cmd-I",
                toggleCodeBlock: "Cmd-Alt-C",
                togglePreview: "Cmd-P",
                toggleSideBySide: "F9",
                toggleFullScreen: "F11"
            }
        });

        // Nastavení obsahu
        if (initialValue) {
            easyMDE.value(initialValue);
        }

        // Event listener pro změny obsahu
        easyMDE.codemirror.on('change', () => {
            const content = easyMDE.value();
            dotNetRef.invokeMethodAsync('OnContentChanged', content);
        });

        // Uložíme instanci editoru
        editorInstances.set(textareaId, easyMDE);
        
        console.log(`Markdown editor ${textareaId} byl úspěšně inicializován`);
    } catch (error) {
        console.error('Chyba při vytváření EasyMDE editoru:', error);
    }
}

export function setEditorValue(textareaId, content) {
    const editor = editorInstances.get(textareaId);
    if (editor) {
        editor.value(content || '');
    }
}

// Inserts a block (e.g. a [graf1] ... [/graf1] graph from the graph builder) at the cursor,
// on its own lines with a blank line before and after so it stays a separate paragraph.
export function insertBlockAtCursor(textareaId, text) {
    const editor = editorInstances.get(textareaId);
    if (!editor) return false;

    const doc = editor.codemirror.getDoc();
    const cursor = doc.getCursor();
    const line = doc.getLine(cursor.line) || '';
    const before = line.slice(0, cursor.ch);
    const after = line.slice(cursor.ch);

    const prefix = before.trim().length > 0 ? '\n\n' : (cursor.line > 0 && before.length === 0 && (doc.getLine(cursor.line - 1) || '').trim().length > 0 ? '\n' : '');
    const nextLine = cursor.line + 1 < doc.lineCount() ? doc.getLine(cursor.line + 1) : null;
    const suffix = after.trim().length > 0 ? '\n\n' : (nextLine !== null && nextLine.trim().length > 0 ? '\n' : '');

    doc.replaceRange(prefix + text + suffix, cursor);
    editor.codemirror.focus();
    return true;
}

export function getEditorValue(textareaId) {
    const editor = editorInstances.get(textareaId);
    return editor ? editor.value() : '';
}

export function disposeMarkdownEditor(textareaId) {
    const editor = editorInstances.get(textareaId);
    if (editor) {
        try {
            editor.toTextArea();
            editorInstances.delete(textareaId);
            console.log(`Markdown editor ${textareaId} byl zrušen`);
        } catch (error) {
            console.error('Chyba při rušení editoru:', error);
        }
    }
}

// Highlight grammar issues in the editor. Issues is an array of objects with { originalText, correction, explanation }
export function highlightGrammarErrors(textareaId, issues) {
    const instance = editorInstances.get(textareaId);
    if (!instance) return;
    try {
        const cm = instance.codemirror;
        // clear previous marks first
        clearGrammarHighlights(textareaId);

        const content = cm.getValue();
        issues.forEach((it) => {
            if (!it || !it.originalText) return;
            const needle = it.originalText;
            let startIndex = 0;
            while (true) {
                const found = content.indexOf(needle, startIndex);
                if (found === -1) break;
                const from = cm.posFromIndex(found);
                const to = cm.posFromIndex(found + needle.length);
                // Mark with a class and a title (tooltip showing correction)
                cm.markText(from, to, { className: 'grammar-error', title: `Suggestion: ${it.correction}\n${it.explanation}` });
                startIndex = found + needle.length;
            }
        });
    } catch (e) {
        console.error('Failed to highlight grammar errors', e);
    }
}

export function clearGrammarHighlights(textareaId) {
    const instance = editorInstances.get(textareaId);
    if (!instance) return;
    try {
        const cm = instance.codemirror;
        const marks = cm.getAllMarks();
        marks.forEach(m => m.clear());
    } catch (e) {
        console.error('Failed to clear grammar highlights', e);
    }
}
