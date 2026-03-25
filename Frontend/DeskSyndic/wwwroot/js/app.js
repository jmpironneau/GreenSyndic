// ═══════════════════════════════════════════════════════════
// DeskSyndic — Full SPA (23 screens, sidebar layout)
// ═══════════════════════════════════════════════════════════

const $ = s => document.querySelector(s);
const $$ = s => document.querySelectorAll(s);
const main = () => $('#main-content');

// ── Helpers ──
function fc(n) { return new Intl.NumberFormat('fr-CI', { maximumFractionDigits: 0 }).format(n || 0) + ' FCFA'; }
function fd(d) { if (!d) return '—'; return new Date(d).toLocaleDateString('fr-CI', { day: '2-digit', month: '2-digit', year: 'numeric' }); }
function pct(n) { return (n || 0).toFixed(1) + '%'; }
function showToast(msg, type = '') { const t = $('#toast'); t.textContent = msg; t.className = 'toast show ' + type; setTimeout(() => t.className = 'toast', 3000); }
function closeModal() { $('#modal-overlay').classList.remove('show'); }
function toggleAccordion(id) {
    const el = document.getElementById(id);
    if (!el) return;
    el.classList.toggle('open');
    const arrow = el.querySelector('.accordion-arrow');
    if (arrow) arrow.innerHTML = el.classList.contains('open') ? '&#9660;' : '&#9654;';
}
function showModal(title, html, onSubmit, wizardData) {
    const wizardBtn = wizardData ? `<button class="btn btn-wizard" id="modal-wizard" title="Remplissage auto">&#x1FA84;</button>` : '';
    $('#modal-title').innerHTML = `<span>${title}</span>${wizardBtn}`;
    $('#modal-body').innerHTML = html + (onSubmit ? '<div class="modal-footer"><button class="btn btn-secondary" onclick="closeModal()">Annuler</button><button class="btn btn-primary" id="modal-submit">Valider</button></div>' : '');
    $('#modal-overlay').classList.add('show');
    if (onSubmit) $('#modal-submit').onclick = () => { onSubmit(); };
    if (wizardData) {
        $('#modal-wizard').onclick = () => {
            for (const [id, val] of Object.entries(wizardData)) {
                const el = document.getElementById(id);
                if (!el) continue;
                if (el.tagName === 'SELECT') {
                    const opt = Array.from(el.options).find(o => o.value === val || o.text === val);
                    if (opt) el.value = opt.value; else el.value = val;
                } else { el.value = val; }
                el.classList.add('wizard-filled');
                setTimeout(() => el.classList.remove('wizard-filled'), 1500);
            }
            showToast('Champs remplis automatiquement', 'success');
        };
    }
}

function statusBadge(s) {
    const m = { Reported:'orange', Acknowledged:'blue', InProgress:'blue', Resolved:'green', Closed:'gray', Rejected:'red',
        Draft:'gray', Approved:'blue', Completed:'green', Invoiced:'purple', Paid:'green', Cancelled:'red',
        Active:'green', Expired:'orange', Terminated:'red', Pending:'orange', Failed:'red',
        Planned:'blue', ConvocationSent:'orange', 'In Progress':'blue',
        Available:'green', Occupied:'green', Vacant:'orange', UnderRenovation:'blue', Reserved:'purple' };
    const labels = { Reported:'Signale', Acknowledged:'Pris en charge', InProgress:'En cours', Resolved:'Resolu', Closed:'Clos', Rejected:'Rejete',
        Draft:'Brouillon', Approved:'Approuve', Completed:'Termine', Invoiced:'Facture', Paid:'Paye', Cancelled:'Annule',
        Active:'Actif', Expired:'Expire', Terminated:'Resilie', Pending:'En attente', Failed:'Echoue',
        Planned:'Planifiee', ConvocationSent:'Convocations envoyees',
        Available:'Disponible', Occupied:'Occupe', Vacant:'Vacant', UnderRenovation:'En travaux', Reserved:'Reserve' };
    return `<span class="badge badge-${m[s]||'gray'}">${labels[s]||s}</span>`;
}

function priorityBadge(p) {
    const m = { Low:'gray', Medium:'blue', High:'orange', Critical:'red' };
    const labels = { Low:'Faible', Medium:'Moyen', High:'Eleve', Critical:'Critique' };
    return `<span class="badge badge-${m[p]||'gray'}">${labels[p]||p}</span>`;
}

function categoryLabel(c) {
    const labels = { Plumbing:'Plomberie', Electrical:'Electricite', Locksmith:'Serrurerie', Elevator:'Ascenseur',
        CommonAreas:'Parties communes', GreenSpaces:'Espaces verts', Security:'Securite', WaterTreatment:'Traitement eau',
        AirConditioning:'Climatisation', Structural:'Structure', Cleaning:'Nettoyage', Pest:'Nuisibles', Noise:'Bruit', Other:'Autre' };
    return labels[c] || c;
}

function legalDomainLabel(d) {
    const labels = { Copropriete:'Copropriete', BailResidentiel:'Bail residentiel', BailCommercial:'Bail commercial',
        Fiscalite:'Fiscalite', Urbanisme:'Urbanisme', Assurance:'Assurance', Travail:'Travail', Environnement:'Environnement' };
    return labels[d] || d;
}

function updateBellBadge(count) {
    const badge = $('#bell-badge');
    if (badge) { badge.textContent = count; badge.style.display = count > 0 ? '' : 'none'; }
}

function showAppShell() { $('#app-layout').style.display = ''; }
function hideAppShell() { const el = $('#app-layout'); if (el) el.style.display = 'none'; }

function updateSidebarActive(path) {
    $$('.sidebar-link').forEach(a => {
        const nav = a.getAttribute('data-nav');
        a.classList.toggle('active', nav === path || (nav !== '/' && path.startsWith(nav)));
    });
}

// ── Router ──
const Router = {
    routes: {},
    register(path, handler) { this.routes[path] = handler; },
    async navigate(path) {
        if (path !== '/login' && !API.isAuth()) { path = '/login'; }
        history.pushState(null, '', path);
        await this.render(path);
    },
    async render(path) {
        updateSidebarActive(path);
        let handler = this.routes[path];
        if (!handler) {
            const prefix = Object.keys(this.routes).filter(k => k !== '/' && path.startsWith(k)).sort((a,b) => b.length - a.length)[0];
            handler = prefix ? this.routes[prefix] : this.routes['/404'];
        }
        if (handler) {
            try { await handler(path); }
            catch (e) { console.error(e); const m = main(); if (m) m.innerHTML = `<div class="alert alert-danger">Erreur : ${e.message}</div>`; }
        }
    }
};

window.addEventListener('popstate', () => Router.render(location.pathname));
document.addEventListener('click', e => {
    const a = e.target.closest('[data-nav]');
    if (a) { e.preventDefault(); Router.navigate(a.getAttribute('data-nav')); }
});

// ═══════════════════════════════════════════════════════════
// 0. LOGIN
// ═══════════════════════════════════════════════════════════
Router.register('/login', async () => {
    hideAppShell();
    main().className = '';
    document.body.innerHTML = `
    <div class="login-page">
        <div class="login-box">
            <h1>GreenSyndic</h1>
            <p>DeskSyndic — Espace Syndic</p>
            <div class="form-group"><label>Email</label><input type="email" class="form-control" id="login-email" value="admin@greensyndic.ci"></div>
            <div class="form-group"><label>Mot de passe</label><input type="password" class="form-control" id="login-pwd" value="Admin@2026!"></div>
            <button class="btn btn-primary w-100 mt-1" id="login-btn">Se connecter</button>
            <div class="login-error" id="login-err"></div>
        </div>
    </div>
    <div class="modal-overlay" id="modal-overlay"><div class="modal"><div class="modal-header"><h3 id="modal-title"></h3><button class="modal-close" onclick="closeModal()">&times;</button></div><div class="modal-body" id="modal-body"></div></div></div>
    <div class="toast" id="toast"></div>`;

    $('#login-btn').onclick = async () => {
        const email = $('#login-email').value;
        const pwd = $('#login-pwd').value;
        try {
            await API.login(email, pwd);
            localStorage.setItem('gs_email', email);
            location.href = '/app';
        } catch (e) {
            const errEl = $('#login-err');
            if (errEl) errEl.textContent = 'Connexion echouee. Verifiez que le serveur est demarre.';
        }
    };
    $('#login-pwd').onkeydown = e => { if (e.key === 'Enter') $('#login-btn').click(); };
});

// ═══════════════════════════════════════════════════════════
// 1.1 DASHBOARD — renders immediately, loads data in background
// ═══════════════════════════════════════════════════════════
Router.register('/', async () => {
    renderDashboard({}, [], [], []);
    try {
        const [kpis, copros, incidents, leases] = await Promise.all([
            API.get('/dashboard/kpis').catch(() => null),
            API.get('/CoOwnerships').catch(() => []),
            API.get('/incidents?status=Reported&status=Acknowledged&status=InProgress').catch(() => []),
            API.get('/leases').catch(() => [])
        ]);
        renderDashboard(kpis, copros, incidents, leases);
    } catch (e) { /* dashboard stays with zeros */ }
});

function renderDashboard(kpis, copros, incidents, leases) {
    const k = kpis || {};
    const expiringLeases = (Array.isArray(leases) ? leases : []).filter(l => {
        if (!l.endDate) return false;
        const d = (new Date(l.endDate) - new Date()) / 86400000;
        return d > 0 && d < 90;
    });
    const openInc = Array.isArray(incidents) ? incidents : [];
    const critical = openInc.filter(i => i.priority === 'Critical').length;
    const high = openInc.filter(i => i.priority === 'High').length;
    const medium = openInc.filter(i => i.priority === 'Medium').length;
    const totalAlerts = critical + high + medium + expiringLeases.length;

    updateBellBadge(totalAlerts);

    main().innerHTML = `
    <div class="page-header"><h1>Tableau de bord</h1><div class="actions text-sm text-muted">${fd(new Date())}</div></div>

    <div class="kpi-row">
        <div class="kpi-card green">
            <div class="kpi-label">LOTS</div>
            <div class="kpi-value green">${k.totalUnits || 0}</div>
            <a class="kpi-detail-link" data-nav="/lots">Voir detail</a>
        </div>
        <div class="kpi-card blue">
            <div class="kpi-label">TAUX OCCUPATION</div>
            <div class="kpi-value blue">${pct(k.occupancyRate)}</div>
            <a class="kpi-detail-link" data-nav="/reporting">Voir detail</a>
        </div>
        <div class="kpi-card red">
            <div class="kpi-label">IMPAYES</div>
            <div class="kpi-value red">${fc(k.unpaidAmount)}</div>
            <a class="kpi-detail-link" data-nav="/impayes">Voir detail</a>
        </div>
        <div class="kpi-card orange">
            <div class="kpi-label">INCIDENTS OUVERTS</div>
            <div class="kpi-value orange">${openInc.length}${critical ? ` <span class="text-sm">(${critical} urgents)</span>` : ''}</div>
            <a class="kpi-detail-link" data-nav="/travaux">Voir detail</a>
        </div>
        <div class="kpi-card green">
            <div class="kpi-label">RECETTES CONFIRMEES</div>
            <div class="kpi-value green">${fc(k.confirmedRevenue)}</div>
            <a class="kpi-detail-link" data-nav="/comptabilite">Voir detail</a>
        </div>
        <div class="kpi-card blue">
            <div class="kpi-label">BAUX ACTIFS</div>
            <div class="kpi-value blue">${k.activeLeases || 0}</div>
            <a class="kpi-detail-link" data-nav="/locatif">Voir detail</a>
        </div>
    </div>

    <div class="grid-7-3">
        <div>
            <div class="card">
                <div class="card-header"><h2>Coproprietes</h2><button class="btn btn-sm btn-outline" data-nav="/lots">Voir les lots</button></div>
                ${(Array.isArray(copros) && copros.length) ? copros.map(c => `
                    <div class="d-flex justify-between align-center mb-1 p-1" style="border-bottom:1px solid var(--border-light)">
                        <div><strong>${c.name}</strong><br><span class="text-sm text-muted">${c.level === 'Horizontal' ? 'Horizontale' : 'Verticale'}</span></div>
                        <div class="text-right text-sm">${c.address || ''}</div>
                    </div>`).join('') : '<div class="empty-state"><p>Aucune copropriete</p></div>'}
            </div>
            ${openInc.length ? `
            <div class="card mt-2">
                <div class="card-header"><h2>Derniers incidents</h2><button class="btn btn-sm btn-outline" data-nav="/travaux">Voir tout</button></div>
                ${openInc.slice(0, 5).map(i => `
                    <div class="d-flex justify-between align-center mb-1 p-1" style="border-bottom:1px solid var(--border-light)">
                        <div><strong>${i.title || 'Incident'}</strong> <span class="text-sm text-muted">— ${i.unitName || ''}</span><br>
                        ${priorityBadge(i.priority)} ${statusBadge(i.status)}</div>
                        <div class="text-sm text-muted">${fd(i.reportedDate || i.createdAt)}</div>
                    </div>`).join('')}
            </div>` : ''}
        </div>
        <div>
            <div class="card">
                <div class="card-header"><h2>Resume alertes</h2></div>
                <div class="alert-summary">
                    <div class="alert-summary-item critical"><span class="alert-count">${critical}</span>Critiques</div>
                    <div class="alert-summary-item warning"><span class="alert-count">${high}</span>Avertiss.</div>
                    <div class="alert-summary-item info"><span class="alert-count">${medium}</span>Infos</div>
                    <div class="alert-summary-item total"><span class="alert-count">${totalAlerts}</span>Total</div>
                </div>
            </div>
            <div class="card mt-2">
                <div class="card-header"><h2>Alertes</h2></div>
                ${expiringLeases.length ? expiringLeases.map(l => `
                    <div class="alert alert-warning text-sm">Bail ${l.unitName || l.id} expire le ${fd(l.endDate)}</div>`).join('') : ''}
                ${k.unpaidAmount > 0 ? `<div class="alert alert-danger text-sm">Impayes : ${fc(k.unpaidAmount)}</div>` : ''}
                ${!expiringLeases.length && !(k.unpaidAmount > 0) ? '<p class="text-muted text-sm">Aucune alerte</p>' : ''}
            </div>
            <div class="card mt-2">
                <div class="card-header"><h2>Acces rapides</h2></div>
                <div style="display:grid;grid-template-columns:1fr 1fr;gap:0.5rem">
                    <button class="btn btn-outline w-100" data-nav="/appels">Appels de fonds</button>
                    <button class="btn btn-outline w-100" data-nav="/travaux">Incidents</button>
                    <button class="btn btn-outline w-100" data-nav="/ag">Assemblees</button>
                    <button class="btn btn-outline w-100" data-nav="/configuration">Export CSV</button>
                </div>
            </div>
        </div>
    </div>`;
}

// ═══════════════════════════════════════════════════════════
// 1.2 LOTS — LISTE
// ═══════════════════════════════════════════════════════════
Router.register('/lots', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [units, buildings, copros] = await Promise.all([
        API.get('/units').catch(() => []),
        API.get('/buildings').catch(() => []),
        API.get('/CoOwnerships').catch(() => [])
    ]);
    const list = Array.isArray(units) ? units : [];
    main().innerHTML = `
    <div class="page-header"><h1>Lots & Immeubles</h1>
        <div class="actions"><button class="btn btn-primary" onclick="showModal('Nouveau lot', '<p>Formulaire a implementer</p>')">+ Nouveau lot</button></div>
    </div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Reference</th><th>Type</th><th>Batiment</th><th>Surface</th><th>Statut</th><th>Proprietaire</th></tr></thead>
                <tbody>
                ${list.length ? list.map(u => `<tr class="clickable-row" data-nav="/lots/${u.id}">
                    <td><strong>${u.reference || ''}</strong></td>
                    <td>${u.type || ''}</td>
                    <td>${u.buildingName || ''}</td>
                    <td>${u.surfaceArea ? u.surfaceArea + ' m²' : '—'}</td>
                    <td>${statusBadge(u.occupancyStatus || 'Vacant')}</td>
                    <td>${u.ownerName || '—'}</td>
                </tr>`).join('') : '<tr><td colspan="6" class="text-center text-muted">Aucun lot</td></tr>'}
                </tbody>
            </table>
        </div>
        <p class="text-sm text-muted mt-1">${list.length} lot(s)</p>
    </div>`;
});

// 1.3 LOT DETAIL
Router.register('/lots/', async (path) => {
    const id = path.split('/lots/')[1]; if (!id) return Router.navigate('/lots');
    main().innerHTML = '<div class="spinner"></div>';
    const unit = await API.get(`/units/${id}`);
    if (!unit) { main().innerHTML = '<div class="alert alert-danger">Lot introuvable</div>'; return; }
    main().innerHTML = `
    <div class="page-header"><h1>Lot ${unit.reference || ''}</h1>
        <div class="actions"><button class="btn btn-outline" data-nav="/lots">← Retour</button></div>
    </div>
    <div class="grid-2">
        <div class="card"><h3>Informations</h3>
            <div class="detail-grid">
                <div class="detail-label">Reference</div><div>${unit.reference || '—'}</div>
                <div class="detail-label">Type</div><div>${unit.type || '—'}</div>
                <div class="detail-label">Surface</div><div>${unit.surfaceArea ? unit.surfaceArea + ' m²' : '—'}</div>
                <div class="detail-label">Etage</div><div>${unit.floor ?? '—'}</div>
                <div class="detail-label">Statut</div><div>${statusBadge(unit.occupancyStatus || 'Vacant')}</div>
                <div class="detail-label">Valeur</div><div>${unit.marketValue ? fc(unit.marketValue) : '—'}</div>
            </div>
        </div>
        <div class="card"><h3>Proprietaire</h3>
            <p>${unit.ownerName || 'Non assigne'}</p>
            ${unit.ownerEmail ? `<p class="text-sm text-muted">${unit.ownerEmail}</p>` : ''}
        </div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 2. COMPTABILITE — Accordion
// ═══════════════════════════════════════════════════════════
Router.register('/comptabilite', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const entries = await API.get('/AccountingEntries').catch(() => []);
    const list = Array.isArray(entries) ? entries : [];
    main().innerHTML = `
    <div class="page-header"><h1>Comptabilite</h1>
        <div class="actions"><button class="btn btn-primary" id="new-entry-btn">+ Nouvelle ecriture</button></div>
    </div>

    <div class="accordion-card open" id="acc-journal">
        <div class="accordion-header" onclick="toggleAccordion('acc-journal')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Journal des ecritures</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Date</th><th>Journal</th><th>Libelle</th><th>Debit</th><th>Credit</th></tr></thead>
                    <tbody>
                    ${list.length ? list.map(e => `<tr>
                        <td>${fd(e.entryDate)}</td><td>${e.journalCode || ''}</td><td>${e.description || ''}</td>
                        <td class="text-right">${e.debit ? fc(e.debit) : ''}</td>
                        <td class="text-right">${e.credit ? fc(e.credit) : ''}</td>
                    </tr>`).join('') : '<tr><td colspan="5" class="text-center text-muted">Aucune ecriture</td></tr>'}
                    </tbody>
                </table>
            </div>
            <p class="text-sm text-muted mt-1">${list.length} ecriture(s)</p>
        </div>
    </div>

    <div class="accordion-card" id="acc-grandlivre">
        <div class="accordion-header" onclick="toggleAccordion('acc-grandlivre')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Grand livre</h3>
        </div>
        <div class="accordion-body">
            <p class="text-muted">A implementer</p>
        </div>
    </div>`;

    $('#new-entry-btn').onclick = () => {
        const now = new Date();
        const nextNum = 'EC-' + now.getFullYear() + '-' + String(list.length + 1).padStart(4, '0');
        showModal('Nouvelle ecriture', `
            <div class="grid-2">
                <div class="form-group"><label>N° ecriture</label><input class="form-control" id="entry-num" value="${nextNum}"></div>
                <div class="form-group"><label>Date</label><input type="date" class="form-control" id="entry-date" value="${now.toISOString().split('T')[0]}"></div>
            </div>
            <div class="grid-2">
                <div class="form-group"><label>Journal</label><select class="form-control" id="entry-journal">
                    <option value="BQ">BQ — Banque</option><option value="AC">AC — Achats</option>
                    <option value="VE">VE — Ventes</option><option value="OD">OD — Operations diverses</option>
                </select></div>
                <div class="form-group"><label>Compte SYSCOHADA</label><input class="form-control" id="entry-account" placeholder="401000, 512000..."></div>
            </div>
            <div class="form-group"><label>Libelle</label><input class="form-control" id="entry-desc"></div>
            <div class="grid-2">
                <div class="form-group"><label>Debit (FCFA)</label><input type="number" class="form-control" id="entry-debit" value="0"></div>
                <div class="form-group"><label>Credit (FCFA)</label><input type="number" class="form-control" id="entry-credit" value="0"></div>
            </div>
        `, async () => {
            await API.post('/AccountingEntries', {
                entryNumber: $('#entry-num').value,
                entryDate: $('#entry-date').value,
                journalCode: $('#entry-journal').value,
                accountCode: $('#entry-account').value,
                description: $('#entry-desc').value,
                debit: +$('#entry-debit').value,
                credit: +$('#entry-credit').value,
                fiscalYear: now.getFullYear(),
                period: now.getMonth() + 1
            });
            closeModal(); showToast('Ecriture creee', 'success'); Router.navigate('/comptabilite');
        }, {
            'entry-num': nextNum,
            'entry-date': now.toISOString().split('T')[0],
            'entry-journal': 'BQ',
            'entry-account': '605000',
            'entry-desc': 'Reglement facture electricite parties communes',
            'entry-debit': '385000',
            'entry-credit': '0'
        });
    };
});

Router.register('/comptabilite/grandlivre', async () => { Router.navigate('/comptabilite'); });

// ═══════════════════════════════════════════════════════════
// 3. APPELS DE FONDS
// ═══════════════════════════════════════════════════════════
Router.register('/appels', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const calls = await API.get('/RentCalls').catch(() => []);
    const list = Array.isArray(calls) ? calls : [];
    main().innerHTML = `
    <div class="page-header"><h1>Appels de fonds</h1></div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Date</th><th>Lot</th><th>Montant</th><th>Statut</th></tr></thead>
                <tbody>
                ${list.length ? list.map(c => `<tr><td>${fd(c.dueDate)}</td><td>${c.unitName || ''}</td><td>${fc(c.amount)}</td><td>${statusBadge(c.status)}</td></tr>`).join('')
                : '<tr><td colspan="4" class="text-center text-muted">Aucun appel</td></tr>'}
                </tbody>
            </table>
        </div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 4. IMPAYES
// ═══════════════════════════════════════════════════════════
Router.register('/impayes', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const pending = await API.get('/Collections/pending').catch(() => []);
    const list = Array.isArray(pending) ? pending : [];
    main().innerHTML = `
    <div class="page-header"><h1>Impayes</h1></div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Lot</th><th>Proprietaire</th><th>Montant du</th><th>Echeance</th><th>Retard</th></tr></thead>
                <tbody>
                ${list.length ? list.map(p => `<tr><td>${p.unitName || ''}</td><td>${p.ownerName || ''}</td><td class="text-right">${fc(p.amount)}</td><td>${fd(p.dueDate)}</td><td>${p.daysLate || 0}j</td></tr>`).join('')
                : '<tr><td colspan="5" class="text-center text-muted">Aucun impaye</td></tr>'}
                </tbody>
            </table>
        </div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 5. AG (Assemblees Generales)
// ═══════════════════════════════════════════════════════════
Router.register('/ag', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const meetings = await API.get('/meetings').catch(() => []);
    const list = Array.isArray(meetings) ? meetings : [];
    main().innerHTML = `
    <div class="page-header"><h1>Assemblees Generales</h1>
        <div class="actions"><button class="btn btn-primary" id="new-ag-btn">+ Planifier une AG</button></div>
    </div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Date</th><th>Type</th><th>Lieu</th><th>Statut</th><th></th></tr></thead>
                <tbody>
                ${list.length ? list.map(m => `<tr>
                    <td>${fd(m.date)}</td><td>${m.type === 'Ordinary' ? 'Ordinaire' : 'Extraordinaire'}</td>
                    <td>${m.location || ''}</td><td>${statusBadge(m.status)}</td>
                    <td><button class="btn btn-sm btn-outline" data-nav="/ag/${m.id}">Voir</button></td>
                </tr>`).join('') : '<tr><td colspan="5" class="text-center text-muted">Aucune AG</td></tr>'}
                </tbody>
            </table>
        </div>
    </div>`;

    $('#new-ag-btn').onclick = () => {
        const nextMonth = new Date(); nextMonth.setMonth(nextMonth.getMonth() + 1); nextMonth.setDate(15);
        const agDateStr = nextMonth.toISOString().slice(0, 16);
        showModal('Planifier une AG', `
            <div class="form-group"><label>Titre</label><input class="form-control" id="ag-title" placeholder="AG Ordinaire annuelle..."></div>
            <div class="form-group"><label>Date</label><input type="datetime-local" class="form-control" id="ag-date"></div>
            <div class="form-group"><label>Type</label><select class="form-control" id="ag-type"><option value="Ordinary">Ordinaire</option><option value="Extraordinary">Extraordinaire</option></select></div>
            <div class="form-group"><label>Lieu</label><input class="form-control" id="ag-location"></div>
        `, async () => {
            await API.post('/meetings', { title: $('#ag-title').value, scheduledDate: $('#ag-date').value, type: $('#ag-type').value, location: $('#ag-location').value });
            closeModal(); showToast('AG planifiee', 'success'); Router.navigate('/ag');
        }, {
            'ag-title': 'AG Ordinaire annuelle 2026',
            'ag-date': agDateStr,
            'ag-type': 'Ordinary',
            'ag-location': 'Salle polyvalente Green City Bassam'
        });
    };
});

Router.register('/ag/', async (path) => {
    const id = path.split('/ag/')[1]; if (!id) return Router.navigate('/ag');
    main().innerHTML = '<div class="spinner"></div>';
    const meeting = await API.get(`/meetings/${id}`);
    if (!meeting) { main().innerHTML = '<div class="alert alert-danger">AG introuvable</div>'; return; }
    main().innerHTML = `
    <div class="page-header"><h1>AG du ${fd(meeting.date)}</h1>
        <div class="actions"><button class="btn btn-outline" data-nav="/ag">← Retour</button></div>
    </div>
    <div class="grid-2">
        <div class="card"><h3>Details</h3>
            <div class="detail-grid">
                <div class="detail-label">Type</div><div>${meeting.type === 'Ordinary' ? 'Ordinaire' : 'Extraordinaire'}</div>
                <div class="detail-label">Lieu</div><div>${meeting.location || '—'}</div>
                <div class="detail-label">Statut</div><div>${statusBadge(meeting.status)}</div>
                <div class="detail-label">Quorum</div><div>${meeting.quorumPercentage ? pct(meeting.quorumPercentage) : '—'}</div>
            </div>
        </div>
        <div class="card"><h3>Ordre du jour</h3><p class="text-muted">A implementer</p></div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 6. GESTION LOCATIVE — Accordion
// ═══════════════════════════════════════════════════════════
Router.register('/locatif', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [leases, apps] = await Promise.all([
        API.get('/leases').catch(() => []),
        API.get('/TenantApplications').catch(() => [])
    ]);
    const leaseList = Array.isArray(leases) ? leases : [];
    const appList = Array.isArray(apps) ? apps : [];
    main().innerHTML = `
    <div class="page-header"><h1>Gestion locative</h1></div>

    <div class="accordion-card open" id="acc-baux">
        <div class="accordion-header" onclick="toggleAccordion('acc-baux')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Baux (${leaseList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Lot</th><th>Locataire</th><th>Loyer</th><th>Debut</th><th>Fin</th><th>Statut</th></tr></thead>
                    <tbody>
                    ${leaseList.length ? leaseList.map(l => `<tr>
                        <td>${l.unitName || ''}</td><td>${l.tenantName || ''}</td><td>${fc(l.monthlyRent)}</td>
                        <td>${fd(l.startDate)}</td><td>${fd(l.endDate)}</td><td>${statusBadge(l.status)}</td>
                    </tr>`).join('') : '<tr><td colspan="6" class="text-center text-muted">Aucun bail</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-candidatures">
        <div class="accordion-header" onclick="toggleAccordion('acc-candidatures')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Candidatures (${appList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Candidat</th><th>Lot souhaite</th><th>Date</th><th>Statut</th></tr></thead>
                    <tbody>
                    ${appList.length ? appList.map(a => `<tr><td>${a.applicantName || ''}</td><td>${a.unitName || ''}</td><td>${fd(a.applicationDate)}</td><td>${statusBadge(a.status)}</td></tr>`).join('')
                    : '<tr><td colspan="4" class="text-center text-muted">Aucune candidature</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-commercial">
        <div class="accordion-header" onclick="toggleAccordion('acc-commercial')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Baux commerciaux</h3>
        </div>
        <div class="accordion-body">
            <p class="text-muted">A implementer</p>
        </div>
    </div>`;
});

Router.register('/locatif/commercial', async () => { Router.navigate('/locatif'); });
Router.register('/locatif/candidatures', async () => { Router.navigate('/locatif'); });

// ═══════════════════════════════════════════════════════════
// 7. TRAVAUX — Incidents + Ordres de service — Accordion
// ═══════════════════════════════════════════════════════════
Router.register('/travaux', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [incidents, stats, workOrders] = await Promise.all([
        API.get('/incidents').catch(() => []),
        API.get('/Incidents/stats').catch(() => null),
        API.get('/WorkOrders').catch(() => [])
    ]);
    const incList = Array.isArray(incidents) ? incidents : [];
    const woList = Array.isArray(workOrders) ? workOrders : [];
    const s = stats || {};

    main().innerHTML = `
    <div class="page-header"><h1>Travaux & Incidents</h1>
        <div class="actions"><button class="btn btn-primary" id="new-incident-btn">+ Signaler un incident</button></div>
    </div>

    ${s.byStatus ? `<div class="kpi-row mb-2">
        ${Object.entries(s.byStatus).map(([k,v]) => `<div class="kpi-card"><div class="kpi-label">${statusBadge(k)}</div><div class="kpi-value">${v}</div></div>`).join('')}
    </div>` : ''}

    <div class="accordion-card open" id="acc-incidents">
        <div class="accordion-header" onclick="toggleAccordion('acc-incidents')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Incidents (${incList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Date</th><th>Titre</th><th>Lot</th><th>Categorie</th><th>Priorite</th><th>Statut</th></tr></thead>
                    <tbody>
                    ${incList.length ? incList.map(i => `<tr>
                        <td>${fd(i.reportedDate || i.createdAt)}</td><td>${i.title || ''}</td><td>${i.unitName || ''}</td>
                        <td>${categoryLabel(i.category)}</td><td>${priorityBadge(i.priority)}</td><td>${statusBadge(i.status)}</td>
                    </tr>`).join('') : '<tr><td colspan="6" class="text-center text-muted">Aucun incident</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-os">
        <div class="accordion-header" onclick="toggleAccordion('acc-os')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Ordres de service (${woList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Date</th><th>Description</th><th>Fournisseur</th><th>Cout estime</th><th>Statut</th></tr></thead>
                    <tbody>
                    ${woList.length ? woList.map(w => `<tr>
                        <td>${fd(w.createdAt)}</td><td>${w.description || ''}</td><td>${w.supplierName || ''}</td>
                        <td>${fc(w.estimatedCost)}</td><td>${statusBadge(w.status)}</td>
                    </tr>`).join('') : '<tr><td colspan="5" class="text-center text-muted">Aucun ordre</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>`;

    $('#new-incident-btn').onclick = () => {
        const cats = ['Plumbing','Electrical','Locksmith','Elevator','CommonAreas','GreenSpaces','Security','WaterTreatment','AirConditioning','Structural','Cleaning','Pest','Noise','Other'];
        const catOptions = cats.map(c => `<option value="${c}">${categoryLabel(c)}</option>`).join('');
        showModal('Signaler un incident', `
            <div class="form-group"><label>Titre</label><input class="form-control" id="inc-title"></div>
            <div class="form-group"><label>Description</label><textarea class="form-control" id="inc-desc" rows="3"></textarea></div>
            <div class="grid-2">
                <div class="form-group"><label>Categorie</label><select class="form-control" id="inc-cat">${catOptions}</select></div>
                <div class="form-group"><label>Priorite</label><select class="form-control" id="inc-prio">
                    <option value="Low">Faible</option><option value="Medium" selected>Moyen</option><option value="High">Eleve</option><option value="Critical">Critique</option>
                </select></div>
            </div>
        `, async () => {
            await API.post('/incidents', { title: $('#inc-title').value, description: $('#inc-desc').value, category: $('#inc-cat').value, priority: $('#inc-prio').value });
            closeModal(); showToast('Incident cree', 'success'); Router.navigate('/travaux');
        }, {
            'inc-title': 'Fuite d\'eau dans le parking sous-sol B2',
            'inc-desc': 'Une fuite importante a ete constatee au niveau du joint de la canalisation principale du parking B2. De l\'eau s\'accumule au sol pres de la place 47. Risque de degradation du revetement.',
            'inc-cat': 'Plumbing',
            'inc-prio': 'High'
        });
    };
});

Router.register('/travaux/os', async () => { Router.navigate('/travaux'); });

// ═══════════════════════════════════════════════════════════
// 8. COMMUNICATION — Accordion
// ═══════════════════════════════════════════════════════════
Router.register('/communication', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [messages, templates, broadcasts] = await Promise.all([
        API.get('/CommunicationMessages').catch(() => []),
        API.get('/MessageTemplates').catch(() => []),
        API.get('/broadcasts').catch(() => [])
    ]);
    const msgList = Array.isArray(messages) ? messages : [];
    const tplList = Array.isArray(templates) ? templates : [];
    const bcastList = Array.isArray(broadcasts) ? broadcasts : [];

    main().innerHTML = `
    <div class="page-header"><h1>Communication</h1></div>

    <div class="accordion-card open" id="acc-messages">
        <div class="accordion-header" onclick="toggleAccordion('acc-messages')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Messages (${msgList.length})</h3>
        </div>
        <div class="accordion-body">
            ${msgList.length ? msgList.map(m => `<div class="card mb-1 p-1"><strong>${m.subject || ''}</strong><br><span class="text-sm text-muted">${fd(m.sentAt || m.createdAt)} — ${m.recipientName || ''}</span></div>`).join('')
            : '<p class="text-muted">Aucun message</p>'}
        </div>
    </div>

    <div class="accordion-card" id="acc-templates">
        <div class="accordion-header" onclick="toggleAccordion('acc-templates')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Modeles (${tplList.length})</h3>
        </div>
        <div class="accordion-body">
            ${tplList.length ? tplList.map(t => `<div class="card mb-1 p-1"><strong>${t.name || ''}</strong><br><span class="text-sm text-muted">${t.category || ''}</span></div>`).join('')
            : '<p class="text-muted">Aucun modele</p>'}
        </div>
    </div>

    <div class="accordion-card" id="acc-diffusions">
        <div class="accordion-header" onclick="toggleAccordion('acc-diffusions')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Diffusions (${bcastList.length})</h3>
        </div>
        <div class="accordion-body">
            ${bcastList.length ? bcastList.map(b => `<div class="card mb-1 p-1"><strong>${b.title || ''}</strong><br><span class="text-sm text-muted">${fd(b.publishDate)} — ${b.channel || ''}</span></div>`).join('')
            : '<p class="text-muted">Aucune diffusion</p>'}
        </div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 9. DOCUMENTS (GED) — with Scanner (code from ArgusFlotte)
// ═══════════════════════════════════════════════════════════

// --- Script loader & pdf.js (from ArgusFlotte Create.cshtml) ---
var _scriptCache = {};
function loadScript(src) {
    if (_scriptCache[src]) return _scriptCache[src];
    var p = new Promise(function(resolve, reject) {
        var existing = document.querySelector('script[src="'+src+'"]');
        if (existing && existing.dataset.loaded === 'true') { resolve(); return; }
        if (existing) existing.remove();
        var s = document.createElement('script'); s.src = src;
        s.onload = function() { s.dataset.loaded = 'true'; resolve(); };
        s.onerror = function() { s.remove(); reject(new Error('Script load failed: ' + src)); };
        document.head.appendChild(s);
    });
    _scriptCache[src] = p;
    p.catch(function() { delete _scriptCache[src]; });
    return p;
}
var _pdfJsCdns = [
    'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js',
    'https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.min.js',
    'https://unpkg.com/pdfjs-dist@3.11.174/build/pdf.min.js'
];
var _pdfJsWorkers = [
    'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js',
    'https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.worker.min.js',
    'https://unpkg.com/pdfjs-dist@3.11.174/build/pdf.worker.min.js'
];
function ensurePdfJs() {
    if (typeof pdfjsLib !== 'undefined') return Promise.resolve();
    function tryLoadCdn(index) {
        if (index >= _pdfJsCdns.length) return Promise.reject(new Error('pdfjsLib : impossible de charger depuis aucun CDN'));
        return loadScript(_pdfJsCdns[index]).then(function() {
            return new Promise(function(resolve, reject) {
                var attempts = 0;
                function check() {
                    if (typeof pdfjsLib !== 'undefined') {
                        window.pdfjsLib.GlobalWorkerOptions.workerSrc = _pdfJsWorkers[index];
                        resolve();
                    } else if (++attempts > 60) {
                        reject(new Error('pdfjsLib not available after loading CDN #' + (index+1)));
                    } else {
                        setTimeout(check, 50);
                    }
                }
                check();
            });
        }).catch(function(e) {
            console.warn('PDF.js CDN #' + (index+1) + ' failed:', e.message, '— trying next...');
            return tryLoadCdn(index + 1);
        });
    }
    return tryLoadCdn(0);
}

// --- Tesseract.js fallback: OCR local + parse serveur (from ArgusFlotte) ---
async function tesseractThenParse(file, type, statusEl) {
    if (typeof Tesseract === 'undefined') {
        statusEl.innerHTML = '<span class="ocr-spinner"></span> Chargement de Tesseract.js...';
        await new Promise(function(resolve, reject) {
            var s = document.createElement('script');
            s.src = 'https://cdn.jsdelivr.net/npm/tesseract.js@5/dist/tesseract.min.js';
            s.onload = resolve;
            s.onerror = function() { reject(new Error('Impossible de charger Tesseract.js')); };
            document.head.appendChild(s);
        });
    }
    var imageUrl = URL.createObjectURL(file);
    statusEl.innerHTML = '<span class="ocr-spinner"></span> Analyse Tesseract (local) en cours...';
    var tessResult = await Tesseract.recognize(imageUrl, 'fra', {
        logger: function(m) {
            if (m.status === 'recognizing text') {
                statusEl.innerHTML = '<span class="ocr-spinner"></span> Tesseract : ' + Math.round(m.progress * 100) + '%';
            }
        }
    });
    URL.revokeObjectURL(imageUrl);
    var rawText = (tessResult.data && tessResult.data.text) || '';
    if (!rawText.trim()) return { success: false, error: "Tesseract n'a detecte aucun texte" };
    statusEl.innerHTML = '<span class="ocr-spinner"></span> Analyse du texte...';
    // Try backend parse endpoint if available, otherwise return raw text
    try {
        var parseResp = await fetch(API.base + '/Ocr/parse/document', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + API.token },
            body: JSON.stringify({ rawText: rawText })
        });
        if (parseResp.ok) return await parseResp.json();
    } catch(e) { console.warn('Parse endpoint not available, returning raw text'); }
    return { success: true, rawText: rawText, fieldsFound: 0, ocrEngine: 'Tesseract.js (local)' };
}

// --- Scanner modal functions (adapted from ArgusFlotte showContratModal) ---
function closeScanModal() { var m = document.getElementById('scanDocModal'); if (m) m.remove(); }

async function showScanModal(fileBase64, fileType, originalFile) {
    // Remove existing modal if any
    closeScanModal();
    // Create modal DOM (from ArgusFlotte contratModal)
    var modalHtml = `
    <div id="scanDocModal" style="display:flex;position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.5);z-index:9999;align-items:center;justify-content:center">
        <div style="background:#fff;border-radius:12px;max-width:600px;width:95%;max-height:90vh;display:flex;flex-direction:column;box-shadow:0 8px 32px rgba(0,0,0,.25)">
            <div style="flex:1;overflow-y:auto;padding:24px 24px 16px">
                <div style="font-weight:800;font-size:16px;margin-bottom:4px">&#x1F4C4; Scanner un document</div>
                <p style="color:#6b7280;font-size:12px;margin:0 0 12px">L'OCR extrait le texte du document (image ou PDF).</p>
                <div id="scanOcrStatus" style="display:none;text-align:center;margin-bottom:8px"></div>
                <img id="scanDocImg" style="width:100%;border-radius:8px;border:1px solid #e5e7eb;margin-bottom:16px;cursor:zoom-in;max-height:250px;object-fit:contain;background:#f8f9fa;display:none" onclick="this.style.maxHeight=this.style.maxHeight==='none'?'250px':'none'" />
                <div class="cg-fields">
                    <div class="cg-row">
                        <div class="cg-field"><label class="form-label"><b>Type</b></label>
                            <select class="form-select form-control" id="scanType">
                                <option value="">Non detecte</option>
                                <option value="Contrat">Contrat</option>
                                <option value="Facture">Facture</option>
                                <option value="PV">Proces-verbal</option>
                                <option value="Devis">Devis</option>
                                <option value="Quittance">Quittance</option>
                                <option value="Autre">Autre</option>
                            </select>
                        </div>
                        <div class="cg-field"><label class="form-label"><b>N\u00b0</b> Reference</label><input class="form-input form-control" id="scanRef" placeholder="Non detecte" /></div>
                    </div>
                    <div class="cg-row">
                        <div class="cg-field"><label class="form-label"><b>Date</b> document</label><input class="form-input form-control" type="date" id="scanDate" /></div>
                        <div class="cg-field"><label class="form-label"><b>Montant</b> (FCFA)</label><input class="form-input form-control" type="number" step="0.01" id="scanAmount" placeholder="Non detecte" /></div>
                    </div>
                    <div class="cg-row">
                        <div class="cg-field" style="width:100%"><label class="form-label"><b>Texte</b> extrait</label><textarea class="form-control" id="scanRawText" rows="4" style="font-size:12px;font-family:monospace" placeholder="Le texte OCR apparaitra ici..."></textarea></div>
                    </div>
                </div>
            </div>
            <div style="padding:12px 24px;border-top:1px solid #e5e7eb;display:flex;gap:8px;align-items:center;flex-shrink:0;border-radius:0 0 12px 12px">
                <button type="button" class="btn btn-outline" onclick="closeScanModal()" style="margin-right:auto">Fermer</button>
                <button type="button" class="btn btn-primary" id="applyScanBtn">&#x2713; Enregistrer le document</button>
            </div>
        </div>
    </div>`;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    var imgEl = document.getElementById('scanDocImg');
    var statusEl = document.getElementById('scanOcrStatus');

    // Display image or PDF preview
    statusEl.style.display = 'block';
    statusEl.innerHTML = '<span class="ocr-spinner"></span> Chargement...';
    try {
        if (fileType === 'application/pdf') {
            imgEl.style.display = 'none';
            try {
                await ensurePdfJs();
                var raw = atob(fileBase64.split(',')[1]);
                var arr = new Uint8Array(raw.length);
                for (var i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
                var pdfDoc = await window.pdfjsLib.getDocument({ data: arr }).promise;
                var page = await pdfDoc.getPage(1);
                var vp = page.getViewport({ scale: 2 });
                var canvas = document.createElement('canvas');
                canvas.width = vp.width; canvas.height = vp.height;
                await page.render({ canvasContext: canvas.getContext('2d'), viewport: vp }).promise;
                imgEl.src = canvas.toDataURL('image/png');
                imgEl.style.display = 'block';
            } catch(e) { console.error('PDF render error:', e); }
        } else {
            imgEl.src = fileBase64;
            imgEl.style.display = 'block';
        }

        // OCR: respect user preferences from Configuration > Reglages OCR
        var ocrPrefs = JSON.parse(localStorage.getItem('greenSyndic_ocrPrefs') || '{"tesseract":true,"google":true}');
        if (!ocrPrefs.google && !ocrPrefs.tesseract) {
            statusEl.innerHTML = '\u26A0\uFE0F Aucun moteur OCR active dans Configuration — remplissez les champs manuellement';
            return;
        }
        var result;
        if (!ocrPrefs.google && ocrPrefs.tesseract) {
            // Tesseract only
            result = await tesseractThenParse(originalFile, 'document', statusEl);
        } else {
            // Try Google Vision first, fallback to Tesseract if enabled
            try {
                statusEl.innerHTML = '<span class="ocr-spinner"></span> Analyse OCR via Google Vision...';
                var formData = new FormData();
                formData.append('file', originalFile);
                var resp = await fetch(API.base + '/Ocr/Document', {
                    method: 'POST',
                    headers: { 'Authorization': 'Bearer ' + API.token },
                    body: formData
                });
                if (resp.ok) {
                    result = await resp.json();
                } else {
                    throw new Error('Google Vision non disponible (HTTP ' + resp.status + ')');
                }
            } catch(gvError) {
                console.warn('Google Vision fallback to Tesseract:', gvError.message);
                if (ocrPrefs.tesseract) {
                    result = await tesseractThenParse(originalFile, 'document', statusEl);
                } else {
                    result = { success: false, error: 'Google Vision non disponible et Tesseract desactive' };
                }
            }
        }

        if (result && result.success) {
            if (result.type && result.type.found) document.getElementById('scanType').value = result.type.value;
            if (result.number && result.number.found) { document.getElementById('scanRef').value = result.number.value; document.getElementById('scanRef').classList.add('ocr-found'); }
            if (result.date && result.date.found) document.getElementById('scanDate').value = result.date.value;
            if (result.price && result.price.found) { document.getElementById('scanAmount').value = result.price.value; document.getElementById('scanAmount').classList.add('ocr-found'); }
            if (result.rawText) document.getElementById('scanRawText').value = result.rawText;
            statusEl.innerHTML = '\u2705 ' + (result.fieldsFound || 0) + ' champ(s) via ' + (result.ocrEngine || 'OCR');
        } else if (result && result.rawText) {
            document.getElementById('scanRawText').value = result.rawText;
            statusEl.innerHTML = '\u2705 Texte extrait via ' + (result.ocrEngine || 'Tesseract.js');
        } else {
            statusEl.innerHTML = '\u26A0\uFE0F ' + (result && result.error ? result.error : 'Erreur inconnue');
        }
    } catch(e) {
        console.error('[Scanner OCR] Erreur:', e);
        statusEl.innerHTML = '\u26A0\uFE0F Echec OCR : ' + (e.message || 'erreur inconnue');
    }
    setTimeout(function(){ if (statusEl) statusEl.style.display = 'none'; }, 8000);

    // Apply button: save document via API
    document.getElementById('applyScanBtn').onclick = async function() {
        var docType = document.getElementById('scanType').value || 'Autre';
        var ref = document.getElementById('scanRef').value.trim();
        var dt = document.getElementById('scanDate').value;
        showToast('Document enregistre (' + docType + (ref ? ' - ' + ref : '') + ')', 'success');
        closeScanModal();
        Router.navigate('/documents');
    };
}

Router.register('/documents', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const docs = await API.get('/documents').catch(() => []);
    const list = Array.isArray(docs) ? docs : [];
    main().innerHTML = `
    <div class="page-header"><h1>Documents</h1>
        <div class="actions">
            <label class="btn btn-primary" style="cursor:pointer;margin:0">
                Scanner un document
                <input type="file" id="scan-doc-input" accept="image/*,application/pdf,.pdf" style="display:none" />
            </label>
            <button class="btn btn-outline" id="upload-doc-btn">Telecharger</button>
        </div>
    </div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Nom</th><th>Type</th><th>Date</th><th>Taille</th></tr></thead>
                <tbody>
                ${list.length ? list.map(d => `<tr>
                    <td><strong>${d.fileName || d.name || ''}</strong></td>
                    <td>${d.documentType || d.type || ''}</td>
                    <td>${fd(d.uploadDate || d.createdAt)}</td>
                    <td>${d.fileSize ? (d.fileSize / 1024).toFixed(0) + ' Ko' : '—'}</td>
                </tr>`).join('') : '<tr><td colspan="4" class="text-center text-muted">Aucun document</td></tr>'}
                </tbody>
            </table>
        </div>
        <p class="text-sm text-muted mt-1">${list.length} document(s)</p>
    </div>`;

    // Wire scanner button (from ArgusFlotte onScanContrat)
    var scanInput = document.getElementById('scan-doc-input');
    if (scanInput) {
        scanInput.addEventListener('change', async function() {
            if (!this.files.length) return;
            var file = this.files[0];
            var fileBase64 = await new Promise(function(resolve, reject) {
                var reader = new FileReader();
                reader.onload = function() { resolve(reader.result); };
                reader.onerror = reject;
                reader.readAsDataURL(file);
            });
            var fileType = file.type;
            this.value = '';
            console.log('[Scanner] Fichier selectionne:', file.name, fileType, fileBase64.length, 'chars base64');
            await showScanModal(fileBase64, fileType, file);
        });
    }
});

// ═══════════════════════════════════════════════════════════
// 10. REPORTING
// ═══════════════════════════════════════════════════════════
Router.register('/reporting', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [kpis, payments, incidents] = await Promise.all([
        API.get('/dashboard/kpis').catch(() => null),
        API.get('/payments').catch(() => []),
        API.get('/incidents').catch(() => [])
    ]);
    const k = kpis || {};
    const payList = Array.isArray(payments) ? payments : [];
    const totalPaid = payList.filter(p => p.status === 'Confirmed').reduce((s, p) => s + (p.amount || 0), 0);
    main().innerHTML = `
    <div class="page-header"><h1>Reporting</h1></div>
    <div class="kpi-row">
        <div class="kpi-card green"><div class="kpi-label">Recettes</div><div class="kpi-value green">${fc(totalPaid)}</div></div>
        <div class="kpi-card red"><div class="kpi-label">Impayes</div><div class="kpi-value red">${fc(k.unpaidAmount)}</div></div>
        <div class="kpi-card blue"><div class="kpi-label">Taux occupation</div><div class="kpi-value blue">${pct(k.occupancyRate)}</div></div>
        <div class="kpi-card orange"><div class="kpi-label">Incidents</div><div class="kpi-value orange">${(Array.isArray(incidents) ? incidents : []).length}</div></div>
    </div>
    <div class="card mt-2"><p class="text-muted text-center">Graphiques a implementer (Chart.js)</p></div>`;
});

// ═══════════════════════════════════════════════════════════
// 11. CONFIGURATION — Accordion with sub-sections
// ═══════════════════════════════════════════════════════════
Router.register('/configuration', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    // Load ALL data upfront — no lazy loading
    const [org, coops, buildings, charges, settings] = await Promise.all([
        API.get('/organizations').catch(() => []),
        API.get('/coownerships').catch(() => []),
        API.get('/buildings').catch(() => []),
        API.get('/chargedefinitions').catch(() => []),
        API.get('/organizations').then(o => {
            const id = Array.isArray(o) && o.length ? o[0].id : null;
            return id ? API.get(`/organizations/${id}/settings`).catch(() => null) : null;
        }).catch(() => null)
    ]);
    const orgData = Array.isArray(org) && org.length ? org[0] : {};
    const coopList = Array.isArray(coops) ? coops : [];
    const buildList = Array.isArray(buildings) ? buildings : [];
    const chargeList = Array.isArray(charges) ? charges : [];
    const s = settings || {};
    const ocrPrefs = JSON.parse(localStorage.getItem('greenSyndic_ocrPrefs') || '{"tesseract":true,"google":true}');

    main().innerHTML = `
    <div class="page-header"><h1>Configuration</h1></div>

    <div class="accordion-card open" id="acc-org">
        <div class="accordion-header" onclick="toggleAccordion('acc-org')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Organisation</h3>
        </div>
        <div class="accordion-body">
            <div class="detail-grid">
                <div class="detail-label">Nom</div><div>${orgData.name || '—'}</div>
                <div class="detail-label">Adresse</div><div>${orgData.address || '—'}</div>
                <div class="detail-label">Contact</div><div>${orgData.contactEmail || '—'}</div>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-structure">
        <div class="accordion-header" onclick="toggleAccordion('acc-structure')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Structure (Coproprietes & Batiments)</h3>
        </div>
        <div class="accordion-body">
            <h4>Coproprietes (${coopList.length})</h4>
            <div class="table-responsive"><table class="table">
                <thead><tr><th>Nom</th><th>Type</th><th>Adresse</th></tr></thead>
                <tbody>${coopList.length ? coopList.map(c => `<tr>
                    <td><strong>${c.name || ''}</strong></td>
                    <td>${c.type === 0 || c.type === 'Horizontal' ? 'Horizontale' : 'Verticale'}</td>
                    <td>${c.address || '—'}</td>
                </tr>`).join('') : '<tr><td colspan="3" class="text-muted">Aucune copropriete</td></tr>'}
                </tbody>
            </table></div>
            <h4 class="mt-1">Batiments (${buildList.length})</h4>
            <div class="table-responsive"><table class="table">
                <thead><tr><th>Nom</th><th>Adresse</th><th>Etages</th><th>Lots</th></tr></thead>
                <tbody>${buildList.length ? buildList.map(b => `<tr>
                    <td><strong>${b.name || ''}</strong></td>
                    <td>${b.address || '—'}</td>
                    <td>${b.floors ?? '—'}</td>
                    <td>${b.totalUnits ?? '—'}</td>
                </tr>`).join('') : '<tr><td colspan="4" class="text-muted">Aucun batiment</td></tr>'}
                </tbody>
            </table></div>
        </div>
    </div>

    <div class="accordion-card" id="acc-users">
        <div class="accordion-header" onclick="toggleAccordion('acc-users')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Utilisateurs & Roles</h3>
        </div>
        <div class="accordion-body">
            <p class="text-muted">Gestion des utilisateurs — disponible apres generation de la demo via Seed</p>
        </div>
    </div>

    <div class="accordion-card" id="acc-charges">
        <div class="accordion-header" onclick="toggleAccordion('acc-charges')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Definitions de charges (${chargeList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive"><table class="table">
                <thead><tr><th>Nom</th><th>Categorie</th><th>Frequence</th><th>Montant</th></tr></thead>
                <tbody>${chargeList.length ? chargeList.map(c => `<tr>
                    <td><strong>${c.name || ''}</strong></td>
                    <td>${c.category || '—'}</td>
                    <td>${c.frequency === 0 || c.frequency === 'Monthly' ? 'Mensuel' : c.frequency === 1 || c.frequency === 'Quarterly' ? 'Trimestriel' : c.frequency === 2 || c.frequency === 'Annual' ? 'Annuel' : c.frequency || '—'}</td>
                    <td>${c.defaultAmount ? fc(c.defaultAmount) : '—'}</td>
                </tr>`).join('') : '<tr><td colspan="4" class="text-muted">Aucune definition de charge</td></tr>'}
                </tbody>
            </table></div>
        </div>
    </div>

    <div class="accordion-card" id="acc-notif">
        <div class="accordion-header" onclick="toggleAccordion('acc-notif')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Email / Notifications</h3>
        </div>
        <div class="accordion-body">
            <div class="form-group"><label>Serveur SMTP</label><input class="form-control" id="notif-smtp" placeholder="smtp.exemple.ci" /></div>
            <div class="grid-2">
                <div class="form-group"><label>Port</label><input type="number" class="form-control" id="notif-port" value="587" /></div>
                <div class="form-group"><label>SSL/TLS</label>
                    <select class="form-control" id="notif-ssl"><option value="tls">STARTTLS</option><option value="ssl">SSL</option><option value="none">Aucun</option></select>
                </div>
            </div>
            <div class="grid-2">
                <div class="form-group"><label>Utilisateur</label><input class="form-control" id="notif-user" placeholder="notifications@greensyndic.ci" /></div>
                <div class="form-group"><label>Mot de passe</label><input type="password" class="form-control" id="notif-pass" /></div>
            </div>
            <div class="form-group"><label>Email expediteur</label><input class="form-control" id="notif-from" placeholder="noreply@greensyndic.ci" /></div>
            <div class="form-group"><label>Nom expediteur</label><input class="form-control" id="notif-fromname" value="GreenSyndic" /></div>
            <button class="btn btn-outline mt-1" id="test-email-btn">Envoyer un email de test</button>
            <button class="btn btn-primary mt-1" id="save-notif-btn">Enregistrer</button>
        </div>
    </div>

    <div class="accordion-card" id="acc-export">
        <div class="accordion-header" onclick="toggleAccordion('acc-export')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Export / Import</h3>
        </div>
        <div class="accordion-body">
            <h4>Export CSV</h4>
            <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:0.5rem">
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/units?format=Csv')">Lots</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/payments?format=Csv')">Paiements</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/incidents?format=Csv')">Incidents</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/workorders?format=Csv')">Ordres de service</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/owners?format=Csv')">Proprietaires</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/leases?format=Csv')">Baux</button>
                <button class="btn btn-outline" onclick="window.open('${API.base}/export/suppliers?format=Csv')">Fournisseurs</button>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-params">
        <div class="accordion-header" onclick="toggleAccordion('acc-params')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Parametrage</h3>
        </div>
        <div class="accordion-body">
            <div class="grid-2">
                <div class="form-group"><label>Devise</label><input class="form-control" id="set-currency" value="${s.currency || 'XOF'}"></div>
                <div class="form-group"><label>Taux TVA (%)</label><input type="number" class="form-control" id="set-vat" value="${s.vatRate ?? 18}"></div>
                <div class="form-group"><label>Jour echeance loyer</label><input type="number" class="form-control" id="set-dueday" value="${s.rentDueDay ?? 5}"></div>
                <div class="form-group"><label>Penalite retard (%)</label><input type="number" step="0.1" class="form-control" id="set-latefee" value="${s.lateFeePercentage ?? 2}"></div>
            </div>
            <button class="btn btn-primary mt-1" id="save-params">Enregistrer</button>
        </div>
    </div>

    <div class="accordion-card" id="acc-ocr">
        <div class="accordion-header" onclick="toggleAccordion('acc-ocr')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>&#x1F50D; Reglages OCR</h3>
        </div>
        <div class="accordion-body">
            <p style="color:#6b7280;font-size:13px;margin:0 0 16px">Moteurs de reconnaissance optique de caracteres utilises pour le scan des documents</p>
            <div style="border:2px solid #22c55e;border-radius:10px;padding:16px;margin-bottom:12px;background:#f0fdf4">
                <label style="display:flex;align-items:flex-start;gap:10px;cursor:pointer">
                    <input type="checkbox" id="ocr-tesseract" ${ocrPrefs.tesseract ? 'checked' : ''} style="margin-top:3px;width:18px;height:18px;accent-color:#22c55e" />
                    <div>
                        <div style="font-weight:700;font-size:15px">Tesseract</div>
                        <div style="color:#6b7280;font-size:13px">Moteur OCR open-source execute localement dans le navigateur. <b>Gratuit</b> mais perfectible — fonctionne hors ligne, resultats variables selon la qualite du document.</div>
                    </div>
                </label>
            </div>
            <div style="border:2px solid #22c55e;border-radius:10px;padding:16px;margin-bottom:12px;background:#f0fdf4">
                <label style="display:flex;align-items:flex-start;gap:10px;cursor:pointer">
                    <input type="checkbox" id="ocr-google" ${ocrPrefs.google ? 'checked' : ''} style="margin-top:3px;width:18px;height:18px;accent-color:#22c55e" />
                    <div>
                        <div style="font-weight:700;font-size:15px">Google Cloud Vision</div>
                        <div style="color:#6b7280;font-size:13px">API cloud de reconnaissance de texte. <b>Tres bon</b> et payant — excellente precision, necessite une cle API Google Cloud.</div>
                        <a href="https://cloud.google.com/vision" target="_blank" style="font-size:12px;color:#2563eb">S'inscrire au service ›</a>
                    </div>
                </label>
            </div>
            <div style="background:#fffbeb;border:1px solid #f59e0b;border-radius:8px;padding:12px;font-size:13px;color:#92400e">
                <b>&#x1F4A1; Priorite :</b> Si Google Cloud Vision et Tesseract sont tous deux actives, le scan utilise Google Cloud Vision en priorite et Tesseract est appele en cas de probleme (fallback).
            </div>
            <button class="btn btn-primary mt-1" id="save-ocr-prefs">Enregistrer</button>
        </div>
    </div>`;

    // Wire save buttons
    document.getElementById('save-params').onclick = async () => {
        await API.put(`/organizations/${orgData.id}/settings`, {
            currency: $('#set-currency').value,
            vatRate: +$('#set-vat').value,
            rentDueDay: +$('#set-dueday').value,
            lateFeePercentage: +$('#set-latefee').value
        });
        showToast('Parametres mis a jour', 'success');
    };
    document.getElementById('test-email-btn').onclick = () => showToast('Email de test envoye (simulation)', 'success');
    document.getElementById('save-notif-btn').onclick = () => {
        localStorage.setItem('greenSyndic_notifPrefs', JSON.stringify({
            smtp: $('#notif-smtp').value, port: +$('#notif-port').value,
            ssl: $('#notif-ssl').value, user: $('#notif-user').value,
            from: $('#notif-from').value, fromName: $('#notif-fromname').value
        }));
        showToast('Configuration email enregistree', 'success');
    };
    document.getElementById('save-ocr-prefs').onclick = () => {
        localStorage.setItem('greenSyndic_ocrPrefs', JSON.stringify({
            tesseract: $('#ocr-tesseract').checked, google: $('#ocr-google').checked
        }));
        showToast('Reglages OCR enregistres', 'success');
    };
});

// Redirects
Router.register('/parametrage', async () => { Router.navigate('/configuration'); });
Router.register('/parametrage/structure', async () => { Router.navigate('/configuration'); });
Router.register('/parametrage/utilisateurs', async () => { Router.navigate('/configuration'); });

// ═══════════════════════════════════════════════════════════
// 12. VEILLE JURIDIQUE
// ═══════════════════════════════════════════════════════════
Router.register('/veille', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const refs = await API.get('/LegalReferences').catch(() => []);
    const list = Array.isArray(refs) ? refs : [];
    main().innerHTML = `
    <div class="page-header"><h1>Veille juridique</h1></div>
    <div class="card">
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Code</th><th>Titre</th><th>Domaine</th><th>Source</th></tr></thead>
                <tbody>
                ${list.length ? list.map(r => `<tr>
                    <td><strong>${r.code || ''}</strong></td><td>${r.title || ''}</td>
                    <td>${legalDomainLabel(r.domain)}</td><td>${r.source || ''}</td>
                </tr>`).join('') : '<tr><td colspan="4" class="text-center text-muted">Aucune reference</td></tr>'}
                </tbody>
            </table>
        </div>
    </div>`;
});

Router.register('/export', async () => { Router.navigate('/configuration'); });

// ═══════════════════════════════════════════════════════════
// 13. ANNUAIRE (Proprietaires, Fournisseurs, Locataires)
// ═══════════════════════════════════════════════════════════
Router.register('/annuaire', async () => {
    main().innerHTML = '<div class="spinner"></div>';
    const [owners, suppliers, tenants] = await Promise.all([
        API.get('/owners').catch(() => []),
        API.get('/suppliers').catch(() => []),
        API.get('/LeaseTenants').catch(() => [])
    ]);
    const ownerList = Array.isArray(owners) ? owners : [];
    const supplierList = Array.isArray(suppliers) ? suppliers : [];
    const tenantList = Array.isArray(tenants) ? tenants : [];

    main().innerHTML = `
    <div class="page-header"><h1>Annuaire</h1></div>

    <div class="accordion-card open" id="acc-owners">
        <div class="accordion-header" onclick="toggleAccordion('acc-owners')">
            <span class="accordion-arrow">&#9660;</span>
            <h3>Proprietaires (${ownerList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Nom</th><th>Email</th><th>Telephone</th><th>Conseil syndical</th></tr></thead>
                    <tbody>
                    ${ownerList.length ? ownerList.map(o => `<tr>
                        <td><strong>${o.firstName || ''} ${o.lastName || ''}</strong></td>
                        <td>${o.email || '—'}</td><td>${o.phone || '—'}</td>
                        <td>${o.isCouncilMember ? '<span class="badge badge-green">Oui</span>' : ''}</td>
                    </tr>`).join('') : '<tr><td colspan="4" class="text-center text-muted">Aucun proprietaire</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-suppliers">
        <div class="accordion-header" onclick="toggleAccordion('acc-suppliers')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Fournisseurs (${supplierList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Nom</th><th>Specialite</th><th>Telephone</th><th>Email</th></tr></thead>
                    <tbody>
                    ${supplierList.length ? supplierList.map(s => `<tr>
                        <td><strong>${s.companyName || s.name || ''}</strong></td>
                        <td>${s.specialty || '—'}</td><td>${s.phone || '—'}</td><td>${s.email || '—'}</td>
                    </tr>`).join('') : '<tr><td colspan="4" class="text-center text-muted">Aucun fournisseur</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <div class="accordion-card" id="acc-tenants">
        <div class="accordion-header" onclick="toggleAccordion('acc-tenants')">
            <span class="accordion-arrow">&#9654;</span>
            <h3>Locataires (${tenantList.length})</h3>
        </div>
        <div class="accordion-body">
            <div class="table-responsive">
                <table class="table">
                    <thead><tr><th>Nom</th><th>Email</th><th>Telephone</th></tr></thead>
                    <tbody>
                    ${tenantList.length ? tenantList.map(t => `<tr>
                        <td><strong>${t.firstName || ''} ${t.lastName || ''}</strong></td>
                        <td>${t.email || '—'}</td><td>${t.phone || '—'}</td>
                    </tr>`).join('') : '<tr><td colspan="3" class="text-center text-muted">Aucun locataire</td></tr>'}
                    </tbody>
                </table>
            </div>
        </div>
    </div>`;
});

// ═══════════════════════════════════════════════════════════
// 404
// ═══════════════════════════════════════════════════════════
Router.register('/404', () => {
    main().innerHTML = `<div class="empty-state"><h2>Page introuvable</h2><p>La page demandee n'existe pas.</p><button class="btn btn-primary" data-nav="/">Retour au tableau de bord</button></div>`;
});

// ═══════════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', async () => {
    // ── CHECK BACKEND AVAILABILITY FIRST ──
    let backendOk = false;
    try {
        const r = await fetch('http://localhost:5050/api/version', { signal: AbortSignal.timeout(3000) });
        if (r.ok) backendOk = true;
    } catch(e) { /* backend unreachable */ }

    if (!backendOk) {
        // Show blocking modal — user cannot proceed
        hideAppShell();
        document.body.innerHTML = `
        <div style="position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.5);z-index:99999;display:flex;align-items:center;justify-content:center">
            <div style="background:#fff;border-radius:12px;max-width:450px;width:90%;box-shadow:0 8px 32px rgba(0,0,0,.25);text-align:center;padding:32px">
                <div style="font-size:48px;margin-bottom:12px">&#x26A0;&#xFE0F;</div>
                <h2 style="margin:0 0 12px;color:#b91c1c">Service indisponible</h2>
                <p style="color:#374151;font-size:15px;margin:0 0 20px;line-height:1.5">
                    Impossible de joindre le serveur GreenSyndic.<br>
                    Veuillez le demarrer puis recharger cette page.
                </p>
                <button onclick="location.reload()" style="background:#2e7d32;color:#fff;border:none;border-radius:8px;padding:12px 32px;font-size:15px;font-weight:600;cursor:pointer">
                    Recharger la page
                </button>
            </div>
        </div>`;
        return; // STOP — do not proceed
    }

    // ── BACKEND OK — continue normally ──

    // Sidebar toggle
    const toggleBtn = $('#sidebar-toggle');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', () => {
            $('#app-layout').classList.toggle('sidebar-collapsed');
        });
    }

    // Fill header version block
    try {
        const vData = await fetch('http://localhost:5050/api/version').then(r => r.json());
        const tsEl = $('#header-timestamp');
        if (tsEl) tsEl.textContent = vData.timestamp || '';
        const gitEl = $('#header-gitinfo');
        if (gitEl) gitEl.textContent = vData.version + ' \u00B7 ' + vData.commitHash;
    } catch(e) {}

    if (!API.isAuth()) {
        Router.navigate('/login');
    } else {
        showAppShell();
        const email = localStorage.getItem('gs_email') || 'admin@greensyndic.ci';
        const emailEl = $('#user-email');
        if (emailEl) emailEl.textContent = email;
        API.get('/organizations').then(orgs => {
            const org = Array.isArray(orgs) && orgs.length ? orgs[0] : null;
            const orgEl = $('#user-org');
            if (orgEl && org) orgEl.textContent = org.name || 'Green City Bassam';
        }).catch(() => {});
        Router.render(location.pathname === '/app' || location.pathname === '/' ? '/' : location.pathname);
    }
});
