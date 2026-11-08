// C# renders the results; this file only updates the page without a full reload.
(() => {
    "use strict";

    const form = document.querySelector("#room-search");
    const results = document.querySelector("#room-results");
    const feedback = document.querySelector("#search-feedback");
    if (!form || !results || !feedback || !window.fetch || !window.AbortController) return;

    const initialValues = new Map(new FormData(form));
    let activeRequest;

    function restoreFilters(url) {
        const query = new Map([...url.searchParams].map(([key, value]) => [key.toLowerCase(), value]));
        for (const field of form.elements) {
            if (field.name && initialValues.has(field.name)) {
                field.value = query.get(field.name.toLowerCase()) ?? initialValues.get(field.name);
            }
        }
    }

    async function loadResults(url, { updateHistory = true, focusResults = false } = {}) {
        activeRequest?.abort();
        const request = new AbortController();
        activeRequest = request;
        const timeout = window.setTimeout(() => request.abort(), 15000);
        results.setAttribute("aria-busy", "true");
        feedback.textContent = "Đang tìm phòng phù hợp…";

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" },
                cache: "no-store",
                signal: request.signal
            });
            if (!response.ok) throw new Error("Search failed");
            const document = new DOMParser().parseFromString(await response.text(), "text/html");
            const content = document.querySelector("[data-room-search-results]");
            if (!content) throw new Error("Unexpected response");
            if (activeRequest !== request) return;

            results.replaceChildren(content);
            if (updateHistory && url.href !== window.location.href) {
                history.pushState(null, "", url);
            }
            feedback.textContent = content.querySelector(".validation-summary-errors")
                ? "Vui lòng kiểm tra lại các tiêu chí tìm kiếm bên dưới."
                : `Đã cập nhật kết quả: ${content.dataset.total} phòng phù hợp.`;
            if (focusResults) {
                restoreFilters(url);
                results.focus({ preventScroll: true });
                results.scrollIntoView({ block: "start", behavior: "instant" });
            }
        } catch {
            // A newer request owns the screen; an old response must not replace it.
            if (activeRequest !== request) return;
            const fallback = window.document.createElement("a");
            fallback.href = url.href;
            fallback.textContent = "Mở kết quả bằng trang đầy đủ";
            feedback.replaceChildren("Chưa tải được kết quả. Bạn có thể thử lại hoặc ", fallback, ".");
        } finally {
            window.clearTimeout(timeout);
            if (activeRequest === request) results.setAttribute("aria-busy", "false");
        }
    }

    form.addEventListener("submit", event => {
        event.preventDefault();
        const url = new URL(form.action, window.location.href);
        url.search = new URLSearchParams(new FormData(form)).toString();
        loadResults(url);
    });

    results.addEventListener("click", event => {
        const link = event.target.closest(".pagination-wrap a");
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        loadResults(new URL(link.href), { focusResults: true });
    });

    window.addEventListener("popstate", () => {
        const url = new URL(window.location.href);
        restoreFilters(url);
        loadResults(url, { updateHistory: false });
    });
})();
