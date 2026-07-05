/**
 * EnhancementHub SPA pilot — request detail shell (Phase 27).
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

    function skeleton() {
        contentEl.innerHTML = `
            <div class="placeholder-glow">
                <span class="placeholder col-8 mb-3"></span>
                <span class="placeholder col-12 mb-2"></span>
                <span class="placeholder col-11 mb-4"></span>
                <div class="card-panel p-4"><span class="placeholder col-12"></span></div>
            </div>`;
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
        skeleton();
        try {
            const [detailRes, analysisRes] = await Promise.all([
                fetch(`/web-api/spa/requests/${requestId}`, { credentials: 'include' }),
                fetch(`/web-api/spa/analysis/${requestId}`, { credentials: 'include' })
            ]);

            if (!detailRes.ok) throw new Error('Request not found');
            const detail = await detailRes.json();
            let analysis = null;
            if (analysisRes.ok) analysis = await analysisRes.json();

            const affected = analysis?.affectedApplications?.length ?? 0;
            const dbChanges = analysis?.databaseChangeRecommendations?.length ?? 0;
            const apiChanges = analysis?.apiChangeRecommendations?.length ?? 0;

            contentEl.innerHTML = `
                <header class="mb-4">
                    <h1 class="h3">${escapeHtml(detail.title)}</h1>
                    <p><span class="badge text-bg-secondary">${escapeHtml(detail.status)}</span>
                       <span class="text-muted ms-2">${escapeHtml(detail.submittedByUserName ?? '')}</span></p>
                </header>
                ${analysis ? `
                <section class="card-panel p-4 mb-3">
                    <h2 class="h6 mb-3">Mission control</h2>
                    <div class="mission-control-grid">
                        <div class="stat-card"><div class="label">Risk</div><div class="value fs-5"><span class="badge ${riskBadgeClass(analysis.riskLevel)}">${escapeHtml(analysis.riskLevel)}</span></div></div>
                        <div class="stat-card"><div class="label">Confidence</div><div class="value fs-5">${Math.round((analysis.confidenceScore ?? 0) * 100)}%</div></div>
                        <div class="stat-card"><div class="label">Affected apps</div><div class="value fs-5">${affected}</div></div>
                        <div class="stat-card"><div class="label">DB / API changes</div><div class="value fs-5">${dbChanges} / ${apiChanges}</div></div>
                    </div>
                </section>` : ''}
                <section class="card-panel p-4 mb-3">
                    <h2 class="h6 text-muted text-uppercase">Business description</h2>
                    <p>${escapeHtml(detail.businessDescription)}</p>
                    <h2 class="h6 text-muted text-uppercase mt-3">Desired outcome</h2>
                    <p class="mb-0">${escapeHtml(detail.desiredOutcome)}</p>
                </section>
                ${analysis ? `
                <section class="card-panel p-4 analysis-summary-banner mb-3">
                    <h2 class="h6">AI analysis (v${analysis.version})</h2>
                    <p class="mb-2">${escapeHtml(analysis.featureSummary ?? 'Analysis complete.')}</p>
                    <span class="badge ${riskBadgeClass(analysis.riskLevel)}">${escapeHtml(analysis.riskLevel)} risk</span>
                </section>` : ''}
                <div class="d-flex gap-2">
                    <a href="/EnhancementRequests/Approve" class="btn btn-outline-primary btn-sm">Approval queue</a>
                    <a href="/SystemMap/Index" class="btn btn-outline-secondary btn-sm">System map</a>
                </div>`;

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
