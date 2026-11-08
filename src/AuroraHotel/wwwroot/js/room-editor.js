// Quill edits text in the browser. The existing C# form validates and sanitizes it.
(() => {
    "use strict";
    if (!window.Quill) return;

    for (const wrapper of document.querySelectorAll("[data-rich-editor]")) {
        const textarea = wrapper.querySelector("textarea");
        const toolbar = wrapper.querySelector("[data-editor-toolbar]");
        const surface = wrapper.querySelector("[data-editor-surface]");
        const error = wrapper.querySelector("[data-editor-error]");
        const form = textarea.form;
        if (!form) continue;

        try {
            toolbar.hidden = false;
            surface.hidden = false;
            const editor = new Quill(surface, {
                theme: "snow",
                formats: ["bold", "italic", "header", "list"],
                modules: { toolbar, history: { userOnly: true } },
                placeholder: "Giới thiệu căn phòng của bạn…"
            });
            const hasMarkup = /<\/?(?:p|br|strong|b|em|i|ul|ol|li|h[1-6])\b/i.test(textarea.value);
            const initialContent = hasMarkup ? { html: textarea.value } : { text: textarea.value };
            editor.setContents(editor.clipboard.convert(initialContent), "silent");
            editor.history.clear();
            editor.root.setAttribute("role", "textbox");
            editor.root.setAttribute("aria-multiline", "true");
            editor.root.setAttribute("aria-labelledby", "description-label");
            editor.root.setAttribute("aria-describedby", "description-help");
            textarea.hidden = true;

            const maximumLength = Number(textarea.dataset.valLengthMax) || 12000;
            function syncContent() {
                textarea.value = editor.getText().trim() ? editor.getSemanticHTML().replace(/&nbsp;/g, " ") : "";
                const tooLong = textarea.value.length > maximumLength;
                error.hidden = !tooLong;
                error.textContent = tooLong ? "Mô tả quá dài. Vui lòng rút gọn nội dung hoặc bớt định dạng." : "";
                editor.root.setAttribute("aria-invalid", String(tooLong));
                return !tooLong;
            }

            editor.on("text-change", syncContent);
            form.addEventListener("submit", event => {
                if (!syncContent()) {
                    event.preventDefault();
                    editor.focus();
                }
            });
        } catch {
            // A failed editor must leave the ordinary textarea usable.
            toolbar.hidden = true;
            surface.hidden = true;
            textarea.hidden = false;
        }
    }
})();
