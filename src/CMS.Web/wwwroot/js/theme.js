// Light/dark theme switching using Bootstrap 5.3 color modes. The server renders
// the initial theme from the "cms-theme" cookie; this keeps the cookie and
// localStorage in sync and toggles on demand. Vanilla JS, no dependencies.
(function () {
    const KEY = 'cms-theme';
    const root = document.documentElement;

    function apply(theme) {
        root.setAttribute('data-bs-theme', theme);
        localStorage.setItem(KEY, theme);
        document.cookie = KEY + '=' + theme + ';path=/;max-age=31536000;samesite=lax';
    }

    // Reconcile any client preference with the server-rendered attribute.
    const stored = localStorage.getItem(KEY);
    if (stored && stored !== root.getAttribute('data-bs-theme')) {
        apply(stored);
    }

    document.addEventListener('DOMContentLoaded', function () {
        const toggle = document.getElementById('cmsThemeToggle');
        if (!toggle) return;
        toggle.addEventListener('click', function () {
            const next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            apply(next);
        });
    });
})();
