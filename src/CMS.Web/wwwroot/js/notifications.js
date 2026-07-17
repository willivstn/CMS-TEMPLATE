// Live notifications: loads the bell, connects to the SignalR hub, shows toasts
// on incoming notifications, and supports mark-read / mark-all-read. Depends on
// the Bootstrap bundle, the SignalR client and window.cmsToast (admin.js).
(function () {
    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
    const badge = document.getElementById('cmsNotifBadge');
    const list = document.getElementById('cmsNotifList');
    if (!badge || !list) return;

    const kindFor = t => ({ success: 'success', warning: 'warning', error: 'danger' }[t] || 'primary');

    function setBadge(count) {
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    function render(items) {
        if (!items.length) {
            list.innerHTML = '<div class="text-center text-secondary small py-4">No notifications</div>';
            return;
        }
        list.innerHTML = items.map(function (n) {
            const unread = n.isRead ? '' : 'fw-semibold';
            const dot = n.isRead ? '' : '<span class="badge text-bg-' + kindFor(n.type) + ' rounded-pill me-2">&nbsp;</span>';
            return '<a href="#" class="dropdown-item py-2 border-bottom cms-notif-item" data-id="' + n.id + '" data-link="' + (n.link || '') + '">' +
                '<div class="d-flex align-items-start">' + dot +
                '<div><div class="small ' + unread + '">' + n.title + '</div>' +
                '<div class="text-secondary" style="font-size:.78rem">' + n.message + '</div></div></div></a>';
        }).join('');
    }

    function load() {
        fetch('/Admin/Notifications/Recent', { headers: { 'Accept': 'application/json' } })
            .then(r => r.ok ? r.json() : null)
            .then(data => { if (data) { setBadge(data.unread); render(data.items); } })
            .catch(() => {});
    }

    function post(url) {
        return fetch(url, { method: 'POST', headers: { 'X-CSRF-TOKEN': token } });
    }

    list.addEventListener('click', function (e) {
        const item = e.target.closest('.cms-notif-item');
        if (!item) return;
        e.preventDefault();
        post('/Admin/Notifications/MarkRead?id=' + item.dataset.id).then(load);
        if (item.dataset.link) window.location.href = item.dataset.link;
    });

    const markAll = document.getElementById('cmsNotifMarkAll');
    if (markAll) markAll.addEventListener('click', () => post('/Admin/Notifications/MarkAllRead').then(load));

    load();

    // Live push via SignalR.
    if (window.signalR) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveNotification', function (n) {
            if (window.cmsToast) window.cmsToast('<strong>' + n.title + '</strong><br>' + n.message, kindFor(n.type));
            load();
        });

        connection.start().catch(() => {});
    }
})();
