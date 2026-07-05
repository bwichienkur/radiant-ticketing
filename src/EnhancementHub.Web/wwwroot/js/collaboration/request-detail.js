/**
 * Real-time collaboration on enhancement request detail (Phase 25).
 */
(function () {
    const panel = document.getElementById('collaboration-panel');
    if (!panel || typeof signalR === 'undefined') return;

    const requestId = panel.dataset.requestId;
    const presenceEl = document.getElementById('collaboration-presence');
    const commentsEl = document.getElementById('collaboration-live-comments');

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/request-collaboration')
        .withAutomaticReconnect()
        .build();

    const viewers = new Map();

    function renderPresence() {
        if (!presenceEl) return;
        const names = [...viewers.values()].filter(Boolean);
        presenceEl.textContent = names.length === 0
            ? 'Only you'
            : `Viewing: ${names.join(', ')}`;
    }

    function appendComment(payload) {
        if (!commentsEl) return;
        const item = document.createElement('div');
        item.className = 'border-bottom py-2 collaboration-live-item';
        item.innerHTML = `
            <strong>${escapeHtml(payload.userDisplayName)}</strong>
            ${payload.isInternal ? '<span class="badge text-bg-warning ms-1">Internal</span>' : ''}
            <p class="small mb-0">${escapeHtml(payload.content)}</p>`;
        commentsEl.prepend(item);
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    connection.on('CommentAdded', payload => appendComment(payload));
    connection.on('UserJoined', payload => {
        viewers.set(payload.connectionId, payload.userName);
        renderPresence();
    });
    connection.on('UserLeft', payload => {
        viewers.delete(payload.connectionId);
        renderPresence();
    });
    connection.on('AnalysisUpdated', payload => {
        const banner = document.getElementById('analysis-update-banner');
        if (banner) {
            banner.classList.remove('d-none');
            banner.textContent = `Analysis updated (v${payload.version}). Refresh for latest.`;
        }
    });

    connection.start()
        .then(() => connection.invoke('JoinRequest', requestId))
        .catch(console.error);

    window.addEventListener('beforeunload', () => {
        connection.invoke('LeaveRequest', requestId).catch(() => undefined);
    });
})();
