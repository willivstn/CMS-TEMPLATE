// Admin shell behaviour: responsive sidebar toggle and a reusable toast helper.
// Depends only on the Bootstrap 5 bundle (no jQuery).
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const app = document.getElementById('cmsApp');
        const toggle = document.getElementById('cmsSidebarToggle');

        if (app) {
            const backdrop = document.createElement('div');
            backdrop.className = 'cms-sidebar-backdrop';
            app.appendChild(backdrop);
            backdrop.addEventListener('click', () => app.classList.remove('sidebar-open'));

            const isMobile = () => window.matchMedia('(max-width: 991.98px)').matches;
            if (toggle) {
                toggle.addEventListener('click', function () {
                    app.classList.toggle(isMobile() ? 'sidebar-open' : 'sidebar-collapsed');
                });
            }
        }

        // Reusable toast: window.cmsToast('Saved', 'success')
        window.cmsToast = function (message, kind) {
            const container = document.getElementById('cmsToastContainer');
            if (!container || typeof bootstrap === 'undefined') return;

            const el = document.createElement('div');
            el.className = 'toast align-items-center text-bg-' + (kind || 'primary') + ' border-0';
            el.setAttribute('role', 'alert');
            el.innerHTML =
                '<div class="d-flex">' +
                '<div class="toast-body">' + message + '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
                '</div>';
            container.appendChild(el);

            const toast = new bootstrap.Toast(el, { delay: 4000 });
            toast.show();
            el.addEventListener('hidden.bs.toast', () => el.remove());
        };

        // Reusable confirmation dialog. Add data-cms-confirm="Are you sure?" to a
        // link or a submit button; the action proceeds only after confirmation.
        //   <a href="/delete/5" data-cms-confirm="Delete this item?">Delete</a>
        //   <button type="submit" data-cms-confirm="Delete?">Delete</button>
        let pendingAction = null;
        const modalEl = document.createElement('div');
        modalEl.className = 'modal fade';
        modalEl.tabIndex = -1;
        modalEl.innerHTML =
            '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
            '<div class="modal-header"><h5 class="modal-title">Please confirm</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>' +
            '<div class="modal-body" data-cms-confirm-body></div>' +
            '<div class="modal-footer"><button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>' +
            '<button type="button" class="btn btn-danger btn-sm" data-cms-confirm-ok>Confirm</button></div>' +
            '</div></div>';
        document.body.appendChild(modalEl);
        const confirmModal = typeof bootstrap !== 'undefined' ? new bootstrap.Modal(modalEl) : null;

        document.addEventListener('click', function (e) {
            const trigger = e.target.closest('[data-cms-confirm]');
            if (!trigger || !confirmModal) return;
            e.preventDefault();
            modalEl.querySelector('[data-cms-confirm-body]').textContent = trigger.getAttribute('data-cms-confirm');
            pendingAction = function () {
                if (trigger.tagName === 'A' && trigger.href) {
                    window.location.href = trigger.href;
                } else if (trigger.form) {
                    trigger.form.submit();
                }
            };
            confirmModal.show();
        });

        modalEl.querySelector('[data-cms-confirm-ok]').addEventListener('click', function () {
            confirmModal.hide();
            if (pendingAction) { pendingAction(); pendingAction = null; }
        });
    });
})();
