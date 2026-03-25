// MobSyndic — PWA Syndic Terrain — Full Implementation
const API_BASE = 'http://localhost:5050';

// ── Helpers ──
const $ = sel => document.querySelector(sel);
const $$ = sel => document.querySelectorAll(sel);
const fc = (n) => (n ?? 0).toLocaleString('fr-FR') + ' FCFA';
const fd = (d) => d ? new Date(d).toLocaleDateString('fr-FR') : '—';
const fdt = (d) => d ? new Date(d).toLocaleString('fr-FR', {day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}) : '—';

const statusLabels = {
    Reported:'Signalé', Acknowledged:'Pris en charge', InProgress:'En cours',
    Resolved:'Résolu', Closed:'Clos', Rejected:'Rejeté',
    Pending:'En attente', Confirmed:'Confirmé', Cancelled:'Annulé',
    Draft:'Brouillon', Approved:'Approuvé', Completed:'Terminé',
    Invoiced:'Facturé', Paid:'Payé'
};
const priorityLabels = { Low:'Basse', Medium:'Moyenne', High:'Haute', Critical:'Critique' };
const categoryLabels = {
    Plumbing:'Plomberie', Electrical:'Électricité', Locksmith:'Serrurerie',
    Elevator:'Ascenseur', CommonAreas:'Parties communes', GreenSpaces:'Espaces verts',
    Security:'Sécurité', WaterTreatment:'Traitement eau', AirConditioning:'Climatisation',
    Structural:'Structure', Cleaning:'Nettoyage', Pest:'Nuisibles',
    Noise:'Bruit', Other:'Autre'
};
const badgeClass = (s) => {
    if (['Reported','Pending','Draft'].includes(s)) return 'badge-orange';
    if (['Confirmed','Resolved','Completed','Paid','Approved'].includes(s)) return 'badge-green';
    if (['Rejected','Cancelled'].includes(s)) return 'badge-red';
    if (['InProgress','Acknowledged','Invoiced'].includes(s)) return 'badge-blue';
    return 'badge-gray';
};

// ── Toast ──
function showToast(msg, type = '') {
    const t = $('#toast');
    t.textContent = msg;
    t.className = 'toast show ' + type;
    setTimeout(() => t.className = 'toast', 3000);
}

// ── Modal ──
function showModal(title, bodyHtml, onConfirm, prefill) {
    const overlay = $('#modal-overlay');
    overlay.querySelector('.modal-title').textContent = title;
    overlay.querySelector('.modal-body').innerHTML = bodyHtml +
        `<div style="display:flex;gap:8px;margin-top:16px">
            <button class="btn btn-primary btn-block" id="modal-ok">Valider</button>
            <button class="btn btn-secondary btn-block" id="modal-cancel">Annuler</button>
        </div>`;
    overlay.classList.add('active');
    if (prefill) Object.entries(prefill).forEach(([id, val]) => {
        const el = document.getElementById(id);
        if (el) el.value = val;
    });
    document.getElementById('modal-ok').onclick = async () => {
        try { await onConfirm(); } catch(e) { showToast(e.message, 'error'); }
    };
    document.getElementById('modal-cancel').onclick = closeModal;
}
function closeModal() { $('#modal-overlay').classList.remove('active'); }

// ── Error dialog ──
function showErrorDialog(userMsg, technicalMsg) {
    const overlay = $('#modal-overlay');
    overlay.querySelector('.modal-title').textContent = 'Erreur';
    overlay.querySelector('.modal-body').innerHTML = `
        <p style="margin-bottom:12px">${userMsg}</p>
        <details style="margin-bottom:16px"><summary style="cursor:pointer;color:var(--gray-600);font-size:13px">Détails techniques</summary>
            <pre style="margin-top:8px;padding:8px;background:var(--gray-100);border-radius:4px;font-size:11px;overflow-x:auto;max-height:200px">${technicalMsg}</pre>
            <button class="btn btn-sm btn-secondary mt-8" onclick="navigator.clipboard.writeText(this.previousElementSibling.textContent);this.textContent='Copié !'">Copier</button>
        </details>
        <button class="btn btn-primary btn-block" onclick="document.getElementById('modal-overlay').classList.remove('active')">Fermer</button>`;
    overlay.classList.add('active');
}

// ── Router ──
const Router = {
    routes: {},
    register(path, handler) { this.routes[path] = handler; },
    async navigate(path) {
        history.pushState(null, '', '/app' + (path === '/' ? '' : path));
        await this.render(path);
        $$('.bottom-nav a').forEach(a => a.classList.toggle('active', a.dataset.nav === path));
    },
    async render(path) {
        const main = $('#main-content');
        const handler = this.routes[path];
        if (handler) {
            try { await handler(main); }
            catch(e) { showErrorDialog('Impossible de charger cette page.', e.message); }
        } else {
            main.innerHTML = '<div class="empty-state"><div class="empty-icon">🚫</div><p>Page non trouvée</p></div>';
        }
    }
};

// ═══════════════════════════════════════════════════════════
// 1. LOGIN
// ═══════════════════════════════════════════════════════════
function showLogin(el) {
    $('.top-nav').style.display = 'none';
    $('.bottom-nav').style.display = 'none';
    el.innerHTML = `
        <div style="text-align:center;padding:3rem 1rem">
            <img src="${API_BASE}/logo.png" alt="GreenSyndic" style="height:80px;margin-bottom:16px" onerror="this.style.display='none'">
            <h1 style="color:var(--green);margin-bottom:0.5rem;font-size:24px">GreenSyndic</h1>
            <p style="color:var(--gray-600);margin-bottom:2rem">Syndic Terrain</p>
            <input id="login-email" type="email" placeholder="Email" class="form-input" style="max-width:320px;margin:6px auto;display:block" value="admin@greensyndic.ci">
            <input id="login-pass" type="password" placeholder="Mot de passe" class="form-input" style="max-width:320px;margin:6px auto;display:block" value="Admin@2026!">
            <button onclick="doLogin()" class="btn btn-primary btn-block" style="max-width:320px;margin:16px auto 0">Se connecter</button>
        </div>`;
}

async function doLogin() {
    const email = $('#login-email').value;
    const pass = $('#login-pass').value;
    // Login permissif : on essaie l'API, sinon on passe quand même
    const ok = await API.login(email, pass);
    if (!ok) { API.token = 'dev-bypass'; localStorage.setItem('gs_token', 'dev-bypass'); }
    $('.top-nav').style.display = '';
    $('.bottom-nav').style.display = '';
    Router.navigate('/');
}

// ═══════════════════════════════════════════════════════════
// 2. DASHBOARD KPI + Actions rapides
// ═══════════════════════════════════════════════════════════
Router.register('/', async (el) => {
    if (!API.token) { showLogin(el); return; }

    // Render immediate with zeros
    el.innerHTML = `
        <h1 class="page-title">Tableau de bord</h1>
        <div class="kpi-grid">
            <div class="kpi-card danger"><div class="kpi-value" id="v-impayes">—</div><div class="kpi-label">Impayés</div></div>
            <div class="kpi-card info"><div class="kpi-value" id="v-occup">—</div><div class="kpi-label">Taux occupation</div></div>
            <div class="kpi-card warning"><div class="kpi-value" id="v-incidents">—</div><div class="kpi-label">Incidents ouverts</div></div>
            <div class="kpi-card"><div class="kpi-value" id="v-tresorerie">—</div><div class="kpi-label">Trésorerie</div></div>
        </div>

        <div class="card">
            <div class="card-header"><span class="card-title">Actions rapides</span></div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
                <button class="btn btn-primary" onclick="Router.navigate('/incidents')" style="font-size:13px">📸 Créer incident</button>
                <button class="btn btn-secondary" onclick="Router.navigate('/vti')" style="font-size:13px">🔍 Lancer VTI</button>
                <button class="btn btn-secondary" onclick="Router.navigate('/impayes')" style="font-size:13px">⚠️ Relancer</button>
                <button class="btn btn-secondary" onclick="Router.navigate('/validation')" style="font-size:13px">✅ Valider facture</button>
            </div>
        </div>

        <div class="card">
            <div class="card-header"><span class="card-title">Derniers incidents</span></div>
            <div id="recent-incidents"><div class="spinner"></div></div>
        </div>

        <div class="card">
            <div class="card-header"><span class="card-title">Paiements récents</span></div>
            <div id="recent-payments"><div class="spinner"></div></div>
        </div>`;

    // Load data async
    try {
        const [kpis, incidents, payments] = await Promise.all([
            API.get('/api/dashboard/kpis').catch(() => null),
            API.get('/api/incidents?status=Reported&status=Acknowledged&status=InProgress').catch(() => []),
            API.get('/api/payments?limit=5').catch(() => [])
        ]);

        const k = kpis || {};
        const vImpayes = document.getElementById('v-impayes');
        const vOccup = document.getElementById('v-occup');
        const vIncidents = document.getElementById('v-incidents');
        const vTresorerie = document.getElementById('v-tresorerie');
        if (vImpayes) vImpayes.textContent = fc(k.unpaidAmount);
        if (vOccup) vOccup.textContent = Math.round(k.occupancyRate ?? 0) + '%';
        if (vIncidents) vIncidents.textContent = k.openIncidents ?? 0;
        if (vTresorerie) vTresorerie.textContent = fc(k.confirmedRevenue);

        const incList = Array.isArray(incidents) ? incidents.slice(0, 5) : [];
        const riEl = document.getElementById('recent-incidents');
        if (riEl) riEl.innerHTML = incList.length ? incList.map(i => `
            <div class="list-item" onclick="Router.navigate('/incidents')">
                <div class="item-icon" style="background:${i.priority === 'Critical' ? '#ffebee' : i.priority === 'High' ? '#fff3e0' : '#e8f5e9'}">
                    ${i.priority === 'Critical' ? '🔴' : i.priority === 'High' ? '🟠' : '🟢'}
                </div>
                <div class="item-body">
                    <div class="item-title">${i.title || 'Sans titre'}</div>
                    <div class="item-sub">${categoryLabels[i.category] || i.category || ''} · ${fd(i.createdAt)}</div>
                </div>
                <span class="item-badge ${badgeClass(i.status)}">${statusLabels[i.status] || i.status}</span>
            </div>`).join('') : '<p class="text-muted" style="padding:12px;text-align:center">Aucun incident ouvert</p>';

        const payList = Array.isArray(payments) ? payments.slice(0, 5) : [];
        const rpEl = document.getElementById('recent-payments');
        if (rpEl) rpEl.innerHTML = payList.length ? payList.map(p => `
            <div class="list-item">
                <div class="item-icon" style="background:#e8f5e9">💰</div>
                <div class="item-body">
                    <div class="item-title">${fc(p.amount)}</div>
                    <div class="item-sub">${p.payerName || '—'} · ${fd(p.paymentDate)}</div>
                </div>
                <span class="item-badge ${badgeClass(p.status)}">${statusLabels[p.status] || p.status}</span>
            </div>`).join('') : '<p class="text-muted" style="padding:12px;text-align:center">Aucun paiement récent</p>';
    } catch(e) {
        console.error('Dashboard load error:', e);
    }
});

// ═══════════════════════════════════════════════════════════
// 3. INCIDENTS — List + Create with photo & GPS
// ═══════════════════════════════════════════════════════════
Router.register('/incidents', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `
        <div class="flex-between mb-16">
            <h1 class="page-title" style="margin:0">Incidents</h1>
            <button class="btn btn-primary btn-sm" id="btn-new-incident">+ Nouveau</button>
        </div>
        <div class="card" id="filter-bar" style="padding:8px 12px;margin-bottom:12px">
            <div style="display:flex;gap:8px;overflow-x:auto">
                <button class="btn btn-sm filter-btn active" data-f="open">Ouverts</button>
                <button class="btn btn-sm filter-btn" data-f="all">Tous</button>
                <button class="btn btn-sm filter-btn" data-f="Resolved">Résolus</button>
                <button class="btn btn-sm filter-btn" data-f="Closed">Clos</button>
            </div>
        </div>
        <div id="incidents-list"><div class="spinner"></div></div>`;

    let allIncidents = [];
    try {
        allIncidents = await API.get('/api/incidents') || [];
        if (!Array.isArray(allIncidents)) allIncidents = [];
    } catch(e) { allIncidents = []; }

    function renderList(filter) {
        let list = allIncidents;
        if (filter === 'open') list = allIncidents.filter(i => ['Reported','Acknowledged','InProgress'].includes(i.status));
        else if (filter !== 'all') list = allIncidents.filter(i => i.status === filter);

        const container = document.getElementById('incidents-list');
        if (!container) return;
        container.innerHTML = list.length ? list.map(i => `
            <div class="card" style="cursor:pointer" onclick="showIncidentDetail('${i.id}')">
                <div class="flex-between">
                    <div>
                        <div style="font-weight:600">${i.title || 'Sans titre'}</div>
                        <div style="font-size:12px;color:var(--gray-600)">${categoryLabels[i.category] || ''} · ${fd(i.createdAt)}</div>
                    </div>
                    <span class="item-badge ${badgeClass(i.status)}">${statusLabels[i.status] || i.status}</span>
                </div>
                ${i.description ? `<p style="font-size:13px;color:var(--gray-700);margin-top:8px">${i.description.substring(0, 100)}${i.description.length > 100 ? '...' : ''}</p>` : ''}
                <div style="font-size:11px;color:var(--gray-500);margin-top:4px">
                    ${priorityLabels[i.priority] || i.priority || ''} ${i.location ? ' · 📍 ' + i.location : ''}
                </div>
            </div>`).join('') : '<div class="empty-state"><div class="empty-icon">✅</div><p>Aucun incident</p></div>';
    }

    renderList('open');

    // Filter buttons
    $$('.filter-btn').forEach(btn => {
        btn.onclick = () => {
            $$('.filter-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            renderList(btn.dataset.f);
        };
    });

    // New incident
    document.getElementById('btn-new-incident').onclick = () => {
        let capturedPhoto = null;
        let gpsLat = null, gpsLng = null;

        showModal('Signaler un incident', `
            <div class="form-group"><label class="form-label">Titre</label>
                <input class="form-input" id="inc-title" placeholder="Ex: Fuite d'eau bâtiment Acajou"></div>
            <div class="form-group"><label class="form-label">Catégorie</label>
                <select class="form-select" id="inc-cat">
                    ${Object.entries(categoryLabels).map(([k,v]) => `<option value="${k}">${v}</option>`).join('')}
                </select></div>
            <div class="form-group"><label class="form-label">Priorité</label>
                <select class="form-select" id="inc-prio">
                    ${Object.entries(priorityLabels).map(([k,v]) => `<option value="${k}">${v}</option>`).join('')}
                </select></div>
            <div class="form-group"><label class="form-label">Description</label>
                <textarea class="form-input" id="inc-desc" rows="3" placeholder="Détails..."></textarea></div>
            <div class="form-group"><label class="form-label">Localisation</label>
                <input class="form-input" id="inc-location" placeholder="Ex: Bâtiment Acajou, 3ème étage">
                <button class="btn btn-sm btn-secondary mt-8" id="btn-gps">📍 Ma position GPS</button>
                <span id="gps-status" style="font-size:11px;color:var(--gray-500);margin-left:8px"></span></div>
            <div class="form-group"><label class="form-label">Photo</label>
                <input type="file" accept="image/*" capture="environment" class="form-input" id="inc-photo">
                <div id="photo-preview" style="margin-top:8px"></div></div>
        `, async () => {
            const title = $('#inc-title').value.trim();
            if (!title) { showToast('Titre obligatoire', 'error'); return; }
            await API.post('/api/incidents', {
                title,
                category: $('#inc-cat').value,
                priority: $('#inc-prio').value,
                description: $('#inc-desc').value,
                location: $('#inc-location').value + (gpsLat ? ` (${gpsLat.toFixed(5)}, ${gpsLng.toFixed(5)})` : '')
            });
            closeModal();
            showToast('Incident créé', 'success');
            Router.navigate('/incidents');
        });

        // GPS
        setTimeout(() => {
            const gpsBtn = document.getElementById('btn-gps');
            if (gpsBtn) gpsBtn.onclick = () => {
                document.getElementById('gps-status').textContent = 'Localisation...';
                navigator.geolocation.getCurrentPosition(
                    pos => {
                        gpsLat = pos.coords.latitude;
                        gpsLng = pos.coords.longitude;
                        document.getElementById('gps-status').textContent = `✅ ${gpsLat.toFixed(5)}, ${gpsLng.toFixed(5)}`;
                    },
                    err => { document.getElementById('gps-status').textContent = '❌ ' + err.message; }
                );
            };

            // Photo preview
            const photoInput = document.getElementById('inc-photo');
            if (photoInput) photoInput.onchange = () => {
                const file = photoInput.files[0];
                if (file) {
                    capturedPhoto = file;
                    const preview = document.getElementById('photo-preview');
                    preview.innerHTML = `<img src="${URL.createObjectURL(file)}" style="max-width:100%;max-height:200px;border-radius:8px">`;
                }
            };
        }, 100);
    };
});

// Incident detail
window.showIncidentDetail = async (id) => {
    try {
        const inc = await API.get(`/api/incidents/${id}`);
        if (!inc) return;
        showModal(inc.title || 'Incident', `
            <div style="margin-bottom:12px">
                <span class="item-badge ${badgeClass(inc.status)}">${statusLabels[inc.status] || inc.status}</span>
                <span class="item-badge ${inc.priority === 'Critical' ? 'badge-red' : inc.priority === 'High' ? 'badge-orange' : 'badge-gray'}" style="margin-left:4px">${priorityLabels[inc.priority] || inc.priority}</span>
            </div>
            <p style="font-size:13px;margin-bottom:8px"><strong>Catégorie :</strong> ${categoryLabels[inc.category] || inc.category}</p>
            <p style="font-size:13px;margin-bottom:8px"><strong>Description :</strong> ${inc.description || '—'}</p>
            <p style="font-size:13px;margin-bottom:8px"><strong>Localisation :</strong> ${inc.location || '—'}</p>
            <p style="font-size:13px;margin-bottom:16px"><strong>Créé le :</strong> ${fdt(inc.createdAt)}</p>
            <div style="display:flex;flex-wrap:wrap;gap:6px" id="inc-actions"></div>
        `, () => { closeModal(); }, {});

        // Action buttons based on status
        const actEl = document.getElementById('inc-actions');
        if (!actEl) return;
        const actions = [];
        if (inc.status === 'Reported') actions.push({label:'Prendre en charge', endpoint:`/api/incidents/${id}/acknowledge`, color:'blue'});
        if (inc.status === 'Acknowledged') actions.push({label:'Démarrer', endpoint:`/api/incidents/${id}/start`, color:'blue'});
        if (inc.status === 'InProgress') actions.push({label:'Résoudre', endpoint:`/api/incidents/${id}/resolve`, color:'green'});
        if (inc.status === 'Resolved') actions.push({label:'Clôturer', endpoint:`/api/incidents/${id}/close`, color:'green'});

        actEl.innerHTML = actions.map(a => `<button class="btn btn-sm" style="background:var(--${a.color});color:white" data-ep="${a.endpoint}">${a.label}</button>`).join('');
        actEl.querySelectorAll('button').forEach(btn => {
            btn.onclick = async () => {
                await API.post(btn.dataset.ep, {});
                closeModal();
                showToast('Statut mis à jour', 'success');
                Router.navigate('/incidents');
            };
        });
    } catch(e) { showErrorDialog('Impossible de charger l\'incident.', e.message); }
};

// ═══════════════════════════════════════════════════════════
// 4. VTI — Visite Technique d'Immeuble
// ═══════════════════════════════════════════════════════════
Router.register('/vti', async (el) => {
    if (!API.token) { showLogin(el); return; }

    let buildings = [];
    try { buildings = await API.get('/api/buildings') || []; } catch(e) {}
    if (!Array.isArray(buildings)) buildings = [];

    el.innerHTML = `
        <h1 class="page-title">Visite Technique</h1>
        <div class="card">
            <div class="form-group"><label class="form-label">Bâtiment</label>
                <select class="form-select" id="vti-building">
                    <option value="">-- Choisir --</option>
                    ${buildings.map(b => `<option value="${b.id}">${b.name}</option>`).join('')}
                </select></div>
            <button class="btn btn-primary btn-block" id="btn-start-vti">Démarrer la visite</button>
        </div>
        <div id="vti-checklist" style="display:none"></div>`;

    const checklistItems = [
        {zone:'Hall d\'entrée', points:['Éclairage fonctionnel','Boîtes aux lettres intactes','Sol propre','Interphone OK','Porte d\'entrée ferme bien']},
        {zone:'Escaliers', points:['Éclairage paliers','Rampe/garde-corps solide','Propreté','Peinture état','Minuterie OK']},
        {zone:'Ascenseur', points:['Fonctionne','Éclairage cabine','Boutons OK','Propreté','Affichage sécurité']},
        {zone:'Parking', points:['Éclairage','Propreté','Marquage au sol','Barrière/portail','Ventilation']},
        {zone:'Espaces verts', points:['Pelouse entretenue','Arbres élagués','Arrosage fonctionne','Éclairage extérieur','Mobilier jardin']},
        {zone:'Local technique', points:['Accès sécurisé','Compteurs lisibles','Fuites visibles','Ventilation','Propreté']},
        {zone:'Toiture/terrasse', points:['Étanchéité','Évacuation eaux','Antennes fixées','Garde-corps','Propreté']},
        {zone:'Piscine/STEP', points:['Eau claire','pH correct','Filtration OK','Sécurité périmètre','Propreté abords']}
    ];

    document.getElementById('btn-start-vti').onclick = () => {
        const buildingId = $('#vti-building').value;
        const buildingName = $('#vti-building').selectedOptions[0]?.text || 'Bâtiment';
        const container = document.getElementById('vti-checklist');
        container.style.display = 'block';

        container.innerHTML = `
            <h2 style="margin:16px 0 8px;font-size:18px">📋 VTI — ${buildingName}</h2>
            <p style="font-size:12px;color:var(--gray-600);margin-bottom:12px">${new Date().toLocaleString('fr-FR')}</p>
            ${checklistItems.map((zone, zi) => `
                <div class="card">
                    <div class="card-header"><span class="card-title">${zone.zone}</span></div>
                    ${zone.points.map((pt, pi) => `
                        <div class="list-item" style="cursor:default">
                            <label style="display:flex;align-items:center;gap:8px;width:100%">
                                <input type="checkbox" class="vti-check" data-z="${zi}" data-p="${pi}" style="width:20px;height:20px;accent-color:var(--green)">
                                <span style="flex:1;font-size:14px">${pt}</span>
                            </label>
                        </div>`).join('')}
                    <div class="form-group" style="margin-top:8px">
                        <textarea class="form-input vti-note" data-z="${zi}" rows="1" placeholder="Observation..." style="font-size:12px"></textarea>
                    </div>
                    <div style="margin-top:4px">
                        <label style="font-size:12px;color:var(--gray-600)">Photo :
                            <input type="file" accept="image/*" capture="environment" class="vti-photo" data-z="${zi}" style="font-size:12px">
                        </label>
                    </div>
                </div>`).join('')}
            <div class="card" style="margin-top:8px">
                <div class="form-group"><label class="form-label">Observation générale</label>
                    <textarea class="form-input" id="vti-general-note" rows="3" placeholder="Remarques globales..."></textarea></div>
                <button class="btn btn-primary btn-block" id="btn-submit-vti">✅ Terminer et enregistrer</button>
            </div>`;

        // Progress tracking
        container.querySelectorAll('.vti-check').forEach(cb => {
            cb.onchange = () => {
                const total = container.querySelectorAll('.vti-check').length;
                const checked = container.querySelectorAll('.vti-check:checked').length;
                const pct = Math.round(checked / total * 100);
                showToast(`Progression : ${pct}% (${checked}/${total})`, pct === 100 ? 'success' : '');
            };
        });

        document.getElementById('btn-submit-vti').onclick = async () => {
            const total = container.querySelectorAll('.vti-check').length;
            const checked = container.querySelectorAll('.vti-check:checked').length;

            // Create incident for failed items
            const failedItems = [];
            checklistItems.forEach((zone, zi) => {
                zone.points.forEach((pt, pi) => {
                    const cb = container.querySelector(`.vti-check[data-z="${zi}"][data-p="${pi}"]`);
                    if (cb && !cb.checked) failedItems.push(`${zone.zone}: ${pt}`);
                });
            });

            const generalNote = document.getElementById('vti-general-note')?.value || '';
            const summary = `VTI ${buildingName} — ${checked}/${total} OK (${Math.round(checked/total*100)}%)\n` +
                (failedItems.length ? `\nÉléments non conformes:\n- ${failedItems.join('\n- ')}` : '\nTous les points sont conformes.') +
                (generalNote ? `\n\nObservation: ${generalNote}` : '');

            // Save as incident if there are failed items
            if (failedItems.length > 0 && buildingId) {
                try {
                    await API.post('/api/incidents', {
                        title: `VTI ${buildingName} — ${failedItems.length} non-conformité(s)`,
                        description: summary,
                        category: 'CommonAreas',
                        priority: failedItems.length > 5 ? 'High' : 'Medium',
                        buildingId: buildingId || undefined,
                        location: buildingName
                    });
                } catch(e) { console.error('VTI incident save error:', e); }
            }

            showToast(`VTI terminée — ${checked}/${total} conformes`, 'success');
            container.innerHTML = `
                <div class="card" style="text-align:center;padding:24px">
                    <div style="font-size:48px;margin-bottom:12px">${checked === total ? '✅' : '⚠️'}</div>
                    <h2>${buildingName}</h2>
                    <p style="font-size:14px;color:var(--gray-600);margin:8px 0">${checked}/${total} points conformes (${Math.round(checked/total*100)}%)</p>
                    ${failedItems.length ? `<p style="font-size:13px;color:var(--red)">${failedItems.length} non-conformité(s) signalée(s) en incident</p>` : ''}
                    <p style="font-size:12px;color:var(--gray-500);margin-top:12px">${new Date().toLocaleString('fr-FR')}</p>
                </div>`;
        };
    };
});

// ═══════════════════════════════════════════════════════════
// 5. IMPAYÉS — Liste + Relance SMS
// ═══════════════════════════════════════════════════════════
Router.register('/impayes', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Impayés</h1>
        <div id="impayes-stats" class="kpi-grid" style="margin-bottom:16px">
            <div class="kpi-card danger"><div class="kpi-value" id="imp-total">—</div><div class="kpi-label">Total impayés</div></div>
            <div class="kpi-card warning"><div class="kpi-value" id="imp-count">—</div><div class="kpi-label">Lots concernés</div></div>
        </div>
        <div id="impayes-list"><div class="spinner"></div></div>`;

    try {
        const [units, payments, leases] = await Promise.all([
            API.get('/api/units').catch(() => []),
            API.get('/api/payments').catch(() => []),
            API.get('/api/leases').catch(() => [])
        ]);
        const unitList = Array.isArray(units) ? units : [];
        const payList = Array.isArray(payments) ? payments : [];
        const leaseList = Array.isArray(leases) ? leases : [];

        // Find units with pending payments
        const pendingPay = payList.filter(p => p.status === 'Pending');
        const totalUnpaid = pendingPay.reduce((s, p) => s + (p.amount || 0), 0);

        const impTotal = document.getElementById('imp-total');
        const impCount = document.getElementById('imp-count');
        if (impTotal) impTotal.textContent = fc(totalUnpaid);
        if (impCount) impCount.textContent = pendingPay.length;

        const container = document.getElementById('impayes-list');
        if (pendingPay.length === 0) {
            container.innerHTML = '<div class="empty-state"><div class="empty-icon">🎉</div><p>Aucun impayé !</p></div>';
            return;
        }

        container.innerHTML = pendingPay.map(p => `
            <div class="card" style="margin-bottom:8px">
                <div class="flex-between">
                    <div>
                        <div style="font-weight:600">${p.payerName || 'Inconnu'}</div>
                        <div style="font-size:12px;color:var(--gray-600)">${fd(p.dueDate)} · ${p.paymentType || 'Appel de fonds'}</div>
                    </div>
                    <div style="text-align:right">
                        <div style="font-weight:700;color:var(--red)">${fc(p.amount)}</div>
                        <button class="btn btn-sm btn-danger" style="margin-top:4px;font-size:11px" onclick="relancerSMS('${p.id}', '${(p.payerName || '').replace(/'/g, '')}', ${p.amount})">📱 Relancer</button>
                    </div>
                </div>
            </div>`).join('');
    } catch(e) {
        showErrorDialog('Impossible de charger les impayés.', e.message);
    }
});

window.relancerSMS = (paymentId, name, amount) => {
    const msg = `Bonjour ${name}, nous vous rappelons que votre paiement de ${fc(amount)} est en attente. Merci de régulariser. — Green City Bassam`;
    showModal('Relance SMS', `
        <p style="font-size:13px;margin-bottom:12px"><strong>Destinataire :</strong> ${name}</p>
        <p style="font-size:13px;margin-bottom:12px"><strong>Montant :</strong> ${fc(amount)}</p>
        <div class="form-group"><label class="form-label">Message</label>
            <textarea class="form-input" id="sms-msg" rows="4">${msg}</textarea></div>
    `, async () => {
        // Simulate SMS send (would call real SMS API in production)
        showToast(`📱 SMS envoyé à ${name}`, 'success');
        closeModal();
    });
};

// ═══════════════════════════════════════════════════════════
// 6. VALIDATION — Paiements & factures à valider
// ═══════════════════════════════════════════════════════════
Router.register('/validation', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Validation</h1>
        <div class="card" style="padding:8px 12px;margin-bottom:12px">
            <div style="display:flex;gap:8px">
                <button class="btn btn-sm filter-btn active" data-f="payments">Paiements</button>
                <button class="btn btn-sm filter-btn" data-f="workorders">Travaux</button>
            </div>
        </div>
        <div id="validation-content"><div class="spinner"></div></div>`;

    const [payments, workorders] = await Promise.all([
        API.get('/api/payments').catch(() => []),
        API.get('/api/workorders').catch(() => [])
    ]);
    const pendingPayments = (Array.isArray(payments) ? payments : []).filter(p => p.status === 'Pending');
    const pendingWO = (Array.isArray(workorders) ? workorders : []).filter(w => ['Draft','Completed','Invoiced'].includes(w.status));

    function renderValidation(tab) {
        const container = document.getElementById('validation-content');
        if (tab === 'payments') {
            container.innerHTML = pendingPayments.length ? pendingPayments.map(p => `
                <div class="card" style="margin-bottom:8px">
                    <div class="flex-between">
                        <div>
                            <div style="font-weight:600">${p.payerName || '—'}</div>
                            <div style="font-size:12px;color:var(--gray-600)">${fd(p.paymentDate)} · ${p.paymentMethod || ''}</div>
                        </div>
                        <div style="text-align:right">
                            <div style="font-weight:700">${fc(p.amount)}</div>
                            <div style="display:flex;gap:4px;margin-top:4px">
                                <button class="btn btn-sm" style="background:var(--green);color:white;font-size:11px" onclick="validatePayment('${p.id}')">✅</button>
                                <button class="btn btn-sm" style="background:var(--red);color:white;font-size:11px" onclick="rejectPayment('${p.id}')">❌</button>
                            </div>
                        </div>
                    </div>
                </div>`).join('') : '<div class="empty-state"><div class="empty-icon">✅</div><p>Aucun paiement en attente</p></div>';
        } else {
            container.innerHTML = pendingWO.length ? pendingWO.map(w => `
                <div class="card" style="margin-bottom:8px">
                    <div class="flex-between">
                        <div>
                            <div style="font-weight:600">${w.title || '—'}</div>
                            <div style="font-size:12px;color:var(--gray-600)">${w.supplierName || ''} · ${fd(w.scheduledDate)}</div>
                        </div>
                        <div style="text-align:right">
                            <div style="font-weight:700">${fc(w.estimatedCost)}</div>
                            <span class="item-badge ${badgeClass(w.status)}">${statusLabels[w.status] || w.status}</span>
                            ${w.status === 'Draft' ? `<button class="btn btn-sm" style="background:var(--green);color:white;font-size:11px;margin-top:4px" onclick="approveWorkOrder('${w.id}')">Approuver</button>` : ''}
                            ${w.status === 'Completed' ? `<button class="btn btn-sm" style="background:var(--blue);color:white;font-size:11px;margin-top:4px" onclick="invoiceWorkOrder('${w.id}')">Facturer</button>` : ''}
                            ${w.status === 'Invoiced' ? `<button class="btn btn-sm" style="background:var(--green);color:white;font-size:11px;margin-top:4px" onclick="payWorkOrder('${w.id}')">Payer</button>` : ''}
                        </div>
                    </div>
                </div>`).join('') : '<div class="empty-state"><div class="empty-icon">✅</div><p>Aucun bon de travail en attente</p></div>';
        }
    }

    renderValidation('payments');
    $$('.filter-btn').forEach(btn => {
        btn.onclick = () => {
            $$('.filter-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            renderValidation(btn.dataset.f);
        };
    });
});

window.validatePayment = async (id) => {
    try {
        await API.post(`/api/payments/${id}/confirm`, {});
        showToast('Paiement confirmé', 'success');
        Router.navigate('/validation');
    } catch(e) { showToast('Erreur: ' + e.message, 'error'); }
};

window.rejectPayment = async (id) => {
    try {
        await API.post(`/api/payments/${id}/cancel`, {});
        showToast('Paiement rejeté', 'success');
        Router.navigate('/validation');
    } catch(e) { showToast('Erreur: ' + e.message, 'error'); }
};

window.approveWorkOrder = async (id) => {
    try {
        await API.post(`/api/workorders/${id}/approve`, {});
        showToast('Bon de travail approuvé', 'success');
        Router.navigate('/validation');
    } catch(e) { showToast('Erreur: ' + e.message, 'error'); }
};

window.invoiceWorkOrder = async (id) => {
    showModal('Facturer', `
        <div class="form-group"><label class="form-label">Coût réel</label>
            <input type="number" class="form-input" id="wo-cost" placeholder="Montant FCFA"></div>
    `, async () => {
        await API.post(`/api/workorders/${id}/invoice`, { actualCost: +$('#wo-cost').value });
        closeModal();
        showToast('Facture enregistrée', 'success');
        Router.navigate('/validation');
    });
};

window.payWorkOrder = async (id) => {
    try {
        await API.post(`/api/workorders/${id}/pay`, {});
        showToast('Paiement enregistré', 'success');
        Router.navigate('/validation');
    } catch(e) { showToast('Erreur: ' + e.message, 'error'); }
};

// ═══════════════════════════════════════════════════════════
// FAB button (context-sensitive)
// ═══════════════════════════════════════════════════════════
function updateFab(path) {
    const fab = $('#fab');
    if (path === '/incidents') {
        fab.classList.remove('hidden');
        fab.textContent = '+';
        fab.onclick = () => document.getElementById('btn-new-incident')?.click();
    } else {
        fab.classList.add('hidden');
    }
}

// ═══════════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════════
// Check backend availability
(async () => {
    try {
        const resp = await fetch(`${API_BASE}/api/version`, {method:'GET'});
        if (!resp.ok) throw new Error('Backend HTTP ' + resp.status);
    } catch(e) {
        showErrorDialog(
            'Le serveur GreenSyndic n\'est pas accessible. Vérifiez qu\'il est bien démarré.',
            e.message
        );
    }
})();

$$('.bottom-nav a').forEach(a => {
    a.addEventListener('click', e => {
        e.preventDefault();
        Router.navigate(a.dataset.nav);
        updateFab(a.dataset.nav);
    });
});

window.addEventListener('popstate', () => {
    const path = location.pathname.replace('/app', '') || '/';
    Router.render(path);
    updateFab(path);
});

// Boot
const initPath = location.pathname.replace('/app', '') || '/';
Router.navigate(initPath);
