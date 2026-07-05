/**
 * EnhancementHub global UX (Phase 27): theme, sidebar, command palette, notifications.
 */
(function () {
    const STORAGE_THEME = 'eh-theme';
    const STORAGE_NOTIFICATIONS = 'eh-notifications';

    function initTheme() {
        const saved = localStorage.getItem(STORAGE_THEME);
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const theme = saved || (prefersDark ? 'dark' : 'light');
        document.documentElement.setAttribute('data-bs-theme', theme);
        document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
            btn.setAttribute('aria-pressed', theme === 'dark' ? 'true' : 'false');
            btn.title = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
        });
    }

    function toggleTheme() {
        const html = document.documentElement;
        const next = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        html.setAttribute('data-bs-theme', next);
        localStorage.setItem(STORAGE_THEME, next);
        initTheme();
    }

    function initSidebar() {
        const shell = document.querySelector('.app-shell');
        const toggle = document.querySelector('[data-sidebar-toggle]');
        if (!shell || !toggle) return;

        const collapsed = localStorage.getItem('eh-sidebar-collapsed') === 'true';
        if (collapsed) shell.classList.add('sidebar-collapsed');

        toggle.addEventListener('click', () => {
            shell.classList.toggle('sidebar-collapsed');
            localStorage.setItem('eh-sidebar-collapsed', shell.classList.contains('sidebar-collapsed'));
        });
    }

    function loadNotifications() {
        try {
            return JSON.parse(localStorage.getItem(STORAGE_NOTIFICATIONS) || '[]');
        } catch {
            return [];
        }
    }

    function saveNotifications(items) {
        localStorage.setItem(STORAGE_NOTIFICATIONS, JSON.stringify(items.slice(0, 50)));
        updateNotificationBadge(items);
    }

    function updateNotificationBadge(items) {
        const badge = document.getElementById('notification-badge');
        if (!badge) return;
        const unread = items.filter(n => !n.read).length;
        badge.textContent = unread > 0 ? String(unread) : '';
        badge.classList.toggle('d-none', unread === 0);
    }

    function addNotification(payload) {
        const items = loadNotifications();
        items.unshift({
            id: crypto.randomUUID(),
            title: payload.title ?? 'EnhancementHub',
            message: payload.message ?? '',
            read: false,
            at: new Date().toISOString()
        });
        saveNotifications(items);
        renderNotificationPanel();
    }

    function renderNotificationPanel() {
        const list = document.getElementById('notification-list');
        if (!list) return;
        const items = loadNotifications();
        list.innerHTML = items.length === 0
            ? '<p class="text-muted small p-3 mb-0">No notifications yet.</p>'
            : items.map(n => `
                <div class="notification-item ${n.read ? '' : 'unread'}" data-id="${n.id}">
                    <strong class="d-block">${escapeHtml(n.title)}</strong>
                    <span class="small text-muted">${escapeHtml(n.message)}</span>
                </div>`).join('');
        updateNotificationBadge(items);
    }

    function initNotifications() {
        renderNotificationPanel();
        document.getElementById('notification-mark-read')?.addEventListener('click', () => {
            saveNotifications(loadNotifications().map(n => ({ ...n, read: true })));
            renderNotificationPanel();
        });
        document.getElementById('notification-list')?.addEventListener('click', e => {
            const item = e.target.closest('.notification-item');
            if (!item) return;
            const id = item.dataset.id;
            saveNotifications(loadNotifications().map(n => n.id === id ? { ...n, read: true } : n));
            renderNotificationPanel();
        });

        if (typeof signalR !== 'undefined') {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notifications')
                .withAutomaticReconnect()
                .build();
            connection.on('PlatformNotification', payload => {
                addNotification(payload);
                showToast(payload);
            });
            connection.start().catch(() => undefined);
        }
    }

    function showToast(payload) {
        const container = document.getElementById('notification-toast-container');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = 'toast show mb-2 border-0 shadow';
        toast.innerHTML = `
            <div class="toast-header">
                <strong class="me-auto">${escapeHtml(payload.title ?? 'EnhancementHub')}</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${escapeHtml(payload.message ?? '')}</div>`;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 8000);
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    const commandPages = [
        { title: 'Dashboard', url: '/Index', keys: ['home', 'dashboard'] },
        { title: 'New Request', url: '/EnhancementRequests/Create', keys: ['new', 'create'] },
        { title: 'Requests', url: '/EnhancementRequests/Index', keys: ['requests'] },
        { title: 'Approval Queue', url: '/EnhancementRequests/Approve', keys: ['approve', 'approval'] },
        { title: 'System Map', url: '/SystemMap/Index', keys: ['map', 'graph'] },
        { title: 'Onboarding', url: '/Onboarding/Wizard', keys: ['setup', 'wizard'] },
        { title: 'Applications', url: '/Applications/Index', keys: ['apps'] },
        { title: 'Schema Drift', url: '/SchemaDrift/Index', keys: ['drift'] },
        { title: 'Admin Settings', url: '/Admin/Settings', keys: ['admin', 'settings'] },
        { title: 'Tenancy & Billing', url: '/Admin/Tenancy', keys: ['tenant', 'billing'] }
    ];

    function initCommandPalette() {
        const modal = document.getElementById('commandPalette');
        const input = document.getElementById('commandPaletteInput');
        const results = document.getElementById('commandPaletteResults');
        if (!modal || !input || !results) return;

        const bsModal = bootstrap.Modal.getOrCreateInstance(modal);
        let debounce;

        function renderResults(items) {
            if (items.length === 0) {
                results.innerHTML = '<p class="text-muted small px-3 py-2 mb-0">No results</p>';
                return;
            }
            results.innerHTML = items.map((item, i) => `
                <a href="${item.url}" class="command-result ${i === 0 ? 'active' : ''}" data-url="${item.url}">
                    <span class="command-result-type">${escapeHtml(item.type ?? 'page')}</span>
                    <span class="command-result-title">${escapeHtml(item.title)}</span>
                    <span class="command-result-sub">${escapeHtml(item.subtitle ?? '')}</span>
                </a>`).join('');
        }

        function localSearch(q) {
            const term = q.toLowerCase();
            return commandPages
                .filter(p => p.title.toLowerCase().includes(term) || p.keys.some(k => k.includes(term)))
                .map(p => ({ type: 'page', title: p.title, subtitle: 'Navigate', url: p.url }));
        }

        input.addEventListener('input', () => {
            clearTimeout(debounce);
            const q = input.value.trim();
            debounce = setTimeout(async () => {
                if (q.length < 2) {
                    renderResults(localSearch(q));
                    return;
                }
                try {
                    const res = await fetch(`/web-api/ux/search?q=${encodeURIComponent(q)}`);
                    if (res.ok) renderResults(await res.json());
                } catch {
                    renderResults(localSearch(q));
                }
            }, 200);
        });

        results.addEventListener('click', e => {
            const link = e.target.closest('.command-result');
            if (link) window.location.href = link.dataset.url;
        });

        document.addEventListener('keydown', e => {
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault();
                input.value = '';
                renderResults(commandPages.map(p => ({ type: 'page', title: p.title, subtitle: 'Navigate', url: p.url })));
                bsModal.show();
                setTimeout(() => input.focus(), 100);
            }
        });

        document.querySelector('[data-command-trigger]')?.addEventListener('click', () => {
            input.value = '';
            renderResults(commandPages.map(p => ({ type: 'page', title: p.title, subtitle: 'Navigate', url: p.url })));
            bsModal.show();
            setTimeout(() => input.focus(), 100);
        });
    }

    function initCopilot() {
        const form = document.getElementById('copilot-form');
        const input = document.getElementById('copilot-input');
        const output = document.getElementById('copilot-results');
        if (!form || !input || !output) return;

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const q = input.value.trim();
            if (!q) return;
            output.innerHTML = '<p class="small text-muted mb-0">Searching…</p>';
            try {
                const res = await fetch(`/web-api/ux/copilot?q=${encodeURIComponent(q)}`);
                const data = await res.json();
                const items = data.items ?? data;
                const answer = data.answer ?? '';
                output.innerHTML = `
                    ${answer ? `<p class="small fw-semibold mb-2">${escapeHtml(answer)}</p>` : ''}
                    ${(Array.isArray(items) ? items : []).map(item => `
                        <a href="${item.url}" class="copilot-result d-block">
                            <strong>${escapeHtml(item.title)}</strong>
                            <span class="small text-muted d-block">${escapeHtml(item.subtitle ?? '')}</span>
                        </a>`).join('') || '<p class="small text-muted mb-0">No matches.</p>'}`;
            } catch {
                output.innerHTML = '<p class="small text-danger mb-0">Unable to run query.</p>';
            }
        });
    }

    function initAccordions() {
        document.querySelectorAll('[data-eh-accordion]').forEach(section => {
            const trigger = section.querySelector('[data-eh-accordion-trigger]');
            const body = section.querySelector('[data-eh-accordion-body]');
            if (!trigger || !body) return;
            trigger.addEventListener('click', () => {
                const open = section.classList.toggle('open');
                trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
                body.hidden = !open;
            });
        });
    }

    function initApprovalQueue() {
        const form = document.querySelector('.approval-decision-form');
        if (!form) return;
        form.addEventListener('submit', () => {
            form.querySelectorAll('button[type="submit"]').forEach(btn => {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Submitting…';
            });
        });

        document.addEventListener('keydown', e => {
            if (e.target.matches('input, textarea, select')) return;
            if (e.key === 'j' || e.key === 'k') {
                const items = [...document.querySelectorAll('.approval-queue-item')];
                const active = document.querySelector('.approval-queue-item.active');
                const idx = items.indexOf(active);
                const next = e.key === 'j' ? items[idx + 1] : items[idx - 1];
                next?.click();
            }
        });
    }

    document.querySelectorAll('[data-theme-toggle]').forEach(btn => {
        btn.addEventListener('click', toggleTheme);
    });

    initTheme();
    initSidebar();
    initNotifications();
    initCommandPalette();
    initCopilot();
    initAccordions();
    initApprovalQueue();

    window.EhUx = { toggleTheme, addNotification };
})();
