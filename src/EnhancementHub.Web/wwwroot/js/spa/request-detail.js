/**
 * EnhancementHub SPA pilot — request detail shell (Phase 25).
 * Fetches REST API data and renders without full page reload.
 */
(function () {
    const root = document.getElementById('spa-request-detail');
    if (!root) return;

    const requestId = root.dataset.requestId;
    const statusEl = document.getElementById('spa-status');
    const contentEl = document.getElementById('spa-content');

    function setStatus(message) {
        if (statusEl) statusEl.textContent = message;
    }

    function riskBadgeClass(risk) {
        switch (risk) {
            case 'Critical': return 'text-bg-danger';
            case 'High': return 'text-bg-warning';
            case 'Medium': return 'text-bg-info';
            default: return 'text-bg-success';
        }
    }

    async function load() {
        setStatus('Loading…');
        try {
            const [detailRes, analysisRes] = await Promise.all([
                fetch(`/web-api/spa/requests/${requestId}`, { credentials: 'include' }),
                fetch(`/web-api/spa/analysis/${requestId}`, { credentials: 'include' })
            ]);

            if (!detailRes.ok) throw new Error('Request not found');
            const detail = await detailRes.json();
            let analysis = null;
            if (analysisRes.ok) analysis = await analysisRes.json();

            contentEl.innerHTML = `
                <header class="mb-4">
                    <h1 class="h3">${escapeHtml(detail.title)}</h1>
                    <p><span class="badge text-bg-secondary">${escapeHtml(detail.status)}</span>
                       <span class="text-muted ms-2">${escapeHtml(detail.submittedByUserName ?? '')}</span></p>
                </header>
                <section class="card-panel p-4 mb-3" aria-labelledby="spa-business-heading">
                    <h2 id="spa-business-heading" class="h6 text-muted text-uppercase">Business description</h2>
                    <p>${escapeHtml(detail.businessDescription)}</p>
                    <h2 class="h6 text-muted text-uppercase mt-3">Desired outcome</h2>
                    <p class="mb-0">${escapeHtml(detail.desiredOutcome)}</p>
                </section>
                ${analysis ? `
                <section class="card-panel p-4 analysis-summary-banner" aria-labelledby="spa-analysis-heading">
                    <h2 id="spa-analysis-heading" class="h6">AI analysis (v${analysis.version})</h2>
                    <p class="mb-2">${escapeHtml(analysis.featureSummary ?? 'Analysis complete.')}</p>
                    <span class="badge ${riskBadgeClass(analysis.riskLevel)}">${escapeHtml(analysis.riskLevel)} risk</span>
                    <span class="ms-2 text-muted">Confidence ${Math.round((analysis.confidenceScore ?? 0) * 100)}%</span>
                </section>` : ''}
                <p class="small text-muted mt-3 mb-0">SPA pilot view — data loaded via REST API. See docs/UX_MODERNIZATION.md.</p>`;

            setStatus('');
            contentEl.setAttribute('aria-busy', 'false');
        } catch (err) {
            setStatus('Failed to load request.');
            console.error(err);
        }
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    contentEl.setAttribute('aria-busy', 'true');
    load();
})();
