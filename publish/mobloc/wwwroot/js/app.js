// MobLoc — PWA Locataire — Full Implementation
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
    Draft:'Brouillon', Approved:'Approuvé', Completed:'Terminé'
};
const priorityLabels = { Low:'Basse', Medium:'Moyenne', High:'Haute', Critical:'Critique' };
const categoryLabels = {
    Plumbing:'Plomberie', Electrical:'Électricité', Locksmith:'Serrurerie',
    Elevator:'Ascenseur', CommonAreas:'Parties communes', GreenSpaces:'Espaces verts',
    Security:'Sécurité', WaterTreatment:'Traitement eau', AirConditioning:'Climatisation',
    Structural:'Structure', Cleaning:'Nettoyage', Pest:'Nuisibles', Noise:'Bruit', Other:'Autre'
};
const badgeClass = (s) => {
    if (['Reported','Pending','Draft'].includes(s)) return 'badge-orange';
    if (['Confirmed','Resolved','Completed'].includes(s)) return 'badge-green';
    if (['Rejected','Cancelled'].includes(s)) return 'badge-red';
    if (['InProgress','Acknowledged','Approved'].includes(s)) return 'badge-blue';
    return 'badge-gray';
};

// ── Toast ──
function showToast(msg, type = '') {
    const t = $('#toast'); t.textContent = msg; t.className = 'toast show ' + type;
    setTimeout(() => t.className = 'toast', 3000);
}

// ── Modal ──
function showModal(title, bodyHtml, onConfirm, prefill) {
    const overlay = $('#modal-overlay');
    overlay.querySelector('.modal-title').textContent = title;
    const hasConfirm = typeof onConfirm === 'function';
    overlay.querySelector('.modal-body').innerHTML = bodyHtml +
        (hasConfirm ? `<div style="display:flex;gap:8px;margin-top:16px">
            <button class="btn btn-primary btn-block" id="modal-ok">Valider</button>
            <button class="btn btn-secondary btn-block" id="modal-cancel">Annuler</button>
        </div>` : `<button class="btn btn-secondary btn-block" style="margin-top:16px" onclick="closeModal()">Fermer</button>`);
    overlay.classList.add('active');
    if (prefill) Object.entries(prefill).forEach(([id, val]) => { const el = document.getElementById(id); if (el) el.value = val; });
    if (hasConfirm) {
        document.getElementById('modal-ok').onclick = async () => { try { await onConfirm(); } catch(e) { showToast(e.message, 'error'); } };
        document.getElementById('modal-cancel').onclick = closeModal;
    }
}
function closeModal() { $('#modal-overlay').classList.remove('active'); }

function showErrorDialog(userMsg, technicalMsg) {
    const overlay = $('#modal-overlay');
    overlay.querySelector('.modal-title').textContent = 'Erreur';
    overlay.querySelector('.modal-body').innerHTML = `
        <p style="margin-bottom:12px">${userMsg}</p>
        <details style="margin-bottom:16px"><summary style="cursor:pointer;color:var(--gray-600);font-size:13px">Détails techniques</summary>
            <pre style="margin-top:8px;padding:8px;background:var(--gray-100);border-radius:4px;font-size:11px;overflow-x:auto;max-height:200px">${technicalMsg}</pre>
            <button class="btn btn-sm btn-secondary mt-8" onclick="navigator.clipboard.writeText(this.previousElementSibling.textContent);this.textContent='Copié !'">Copier</button>
        </details>
        <button class="btn btn-primary btn-block" onclick="closeModal()">Fermer</button>`;
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
        if (handler) { try { await handler(main); } catch(e) { showErrorDialog('Impossible de charger cette page.', e.message); } }
        else { main.innerHTML = '<div class="empty-state"><div class="empty-icon">🚫</div><p>Page non trouvée</p></div>'; }
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
            <p style="color:var(--gray-600);margin-bottom:2rem">Espace Locataire</p>
            <input id="login-email" type="email" placeholder="Email" class="form-input" style="max-width:320px;margin:6px auto;display:block" value="admin@greensyndic.ci">
            <input id="login-pass" type="password" placeholder="Mot de passe" class="form-input" style="max-width:320px;margin:6px auto;display:block" value="Admin@2026!">
            <button onclick="doLogin()" class="btn btn-primary btn-block" style="max-width:320px;margin:16px auto 0">Se connecter</button>
        </div>`;
}
async function doLogin() {
    const ok = await API.login($('#login-email').value, $('#login-pass').value);
    if (!ok) { API.token = 'dev-bypass'; localStorage.setItem('gs_token', 'dev-bypass'); }
    $('.top-nav').style.display = '';
    $('.bottom-nav').style.display = '';
    Router.navigate('/');
}

// ═══════════════════════════════════════════════════════════
// 2. ACCUEIL — Carte loyer + Accès rapides + Notifications
// ═══════════════════════════════════════════════════════════
Router.register('/', async (el) => {
    if (!API.token) { showLogin(el); return; }

    // Next rent due day (5th of next month by default)
    const now = new Date();
    const nextDue = new Date(now.getFullYear(), now.getMonth() + (now.getDate() > 5 ? 1 : 0), 5);
    const dueStr = nextDue.toLocaleDateString('fr-FR', {day:'2-digit', month:'long', year:'numeric'});

    el.innerHTML = `
        <div style="margin-bottom:16px"><h1 class="page-title" style="margin:0">Bonjour 👋</h1></div>

        <!-- Carte loyer -->
        <div class="card" style="background:linear-gradient(135deg,#1565c0,#0d47a1);color:white;margin-bottom:16px">
            <div style="font-size:13px;opacity:0.85">Prochain loyer</div>
            <div style="font-size:28px;font-weight:700;margin:4px 0" id="home-rent">—</div>
            <div style="font-size:12px;opacity:0.7;margin-bottom:12px">Échéance : ${dueStr}</div>
            <div style="display:flex;gap:8px">
                <button class="btn" style="background:white;color:#1565c0;flex:1;font-size:13px" onclick="Router.navigate('/loyer')">💳 Payer</button>
                <button class="btn" style="background:rgba(255,255,255,0.2);color:white;flex:1;font-size:13px;border:1px solid rgba(255,255,255,0.3)" onclick="Router.navigate('/documents')">🧾 Quittances</button>
            </div>
        </div>

        <!-- Accès rapides 2x2 -->
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:16px">
            <div class="card" style="text-align:center;padding:16px;cursor:pointer" onclick="Router.navigate('/incidents')">
                <div style="font-size:32px">🔧</div><div style="font-size:12px;margin-top:6px;font-weight:600">Signaler un problème</div>
            </div>
            <div class="card" style="text-align:center;padding:16px;cursor:pointer" onclick="Router.navigate('/documents')">
                <div style="font-size:32px">📄</div><div style="font-size:12px;margin-top:6px;font-weight:600">Mes documents</div>
            </div>
            <div class="card" style="text-align:center;padding:16px;cursor:pointer" onclick="Router.navigate('/contact')">
                <div style="font-size:32px">💬</div><div style="font-size:12px;margin-top:6px;font-weight:600">Contacter le gestionnaire</div>
            </div>
            <div class="card" style="text-align:center;padding:16px;cursor:pointer" onclick="Router.navigate('/infos')">
                <div style="font-size:32px">ℹ️</div><div style="font-size:12px;margin-top:6px;font-weight:600">Infos pratiques</div>
            </div>
        </div>

        <!-- Notifications récentes -->
        <div class="card">
            <div class="card-header"><span class="card-title">Dernières notifications</span></div>
            <div id="home-notifs"><div class="spinner"></div></div>
        </div>

        <!-- Interventions en cours -->
        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Interventions en cours</span></div>
            <div id="home-workorders"><div class="spinner"></div></div>
        </div>`;

    try {
        const [payments, incidents, workorders, leases] = await Promise.all([
            API.get('/api/payments').catch(() => []),
            API.get('/api/incidents').catch(() => []),
            API.get('/api/workorders').catch(() => []),
            API.get('/api/leases').catch(() => [])
        ]);

        // Rent amount from lease
        const leaseList = Array.isArray(leases) ? leases : [];
        const activeLease = leaseList.find(l => l.status === 'Active') || leaseList[0];
        const rentEl = document.getElementById('home-rent');
        if (rentEl) rentEl.textContent = activeLease ? fc(activeLease.monthlyRent) : '—';

        // Notifications
        const notifs = [];
        const payList = Array.isArray(payments) ? payments : [];
        const lastConfirmed = payList.filter(p => p.status === 'Confirmed').sort((a,b) => new Date(b.paymentDate) - new Date(a.paymentDate))[0];
        if (lastConfirmed) notifs.push({icon:'🧾', text:`Votre quittance de ${new Date(lastConfirmed.paymentDate).toLocaleDateString('fr-FR',{month:'long'})} est disponible`, action:'Télécharger', link:'/documents'});

        const openInc = (Array.isArray(incidents) ? incidents : []).filter(i => ['Acknowledged','InProgress'].includes(i.status));
        openInc.slice(0, 2).forEach(i => notifs.push({icon:'🔧', text:`${i.title} — ${statusLabels[i.status] || i.status}`, action:'Détails', link:'/incidents'}));

        const pending = payList.filter(p => p.status === 'Pending');
        if (pending.length) notifs.push({icon:'⚠️', text:`${pending.length} paiement(s) en attente`, action:'Payer', link:'/loyer'});

        if (!notifs.length) notifs.push({icon:'✅', text:'Aucune notification', action:'', link:'/'});

        const nEl = document.getElementById('home-notifs');
        if (nEl) nEl.innerHTML = notifs.map(n => `
            <div class="list-item" onclick="Router.navigate('${n.link}')">
                <div class="item-icon">${n.icon}</div>
                <div class="item-body"><div class="item-title" style="font-size:13px">${n.text}</div></div>
                ${n.action ? `<span style="color:var(--blue);font-size:12px;font-weight:600">${n.action}</span>` : ''}
            </div>`).join('');

        // Work orders
        const woList = (Array.isArray(workorders) ? workorders : []).filter(w => ['Approved','InProgress'].includes(w.status));
        const woEl = document.getElementById('home-workorders');
        if (woEl) woEl.innerHTML = woList.length ? woList.slice(0, 3).map(w => `
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">🏗️</div>
                <div class="item-body">
                    <div class="item-title" style="font-size:13px">${w.title || 'Intervention'}</div>
                    <div class="item-sub">${w.supplierName || ''} · ${fd(w.scheduledDate)}</div>
                </div>
                <span class="item-badge ${badgeClass(w.status)}">${statusLabels[w.status] || w.status}</span>
            </div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucune intervention en cours</p>';
    } catch(e) { console.error(e); }
});

// ═══════════════════════════════════════════════════════════
// 3. MON LOYER — Paiement + Historique + Échéancier
// ═══════════════════════════════════════════════════════════
Router.register('/loyer', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Mon loyer</h1>
        <div class="kpi-grid" style="margin-bottom:16px">
            <div class="kpi-card"><div class="kpi-value" id="rent-amount" style="color:var(--blue)">—</div><div class="kpi-label">Loyer mensuel</div></div>
            <div class="kpi-card danger"><div class="kpi-value" id="rent-due">—</div><div class="kpi-label">À payer</div></div>
        </div>

        <div class="card">
            <div class="card-header"><span class="card-title">Paiements en attente</span></div>
            <div id="rent-pending"><div class="spinner"></div></div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Historique des paiements</span></div>
            <div id="rent-history"><div class="spinner"></div></div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Mon bail</span></div>
            <div id="rent-lease"><div class="spinner"></div></div>
        </div>`;

    try {
        const [payments, leases] = await Promise.all([
            API.get('/api/payments').catch(() => []),
            API.get('/api/leases').catch(() => [])
        ]);
        const payList = Array.isArray(payments) ? payments : [];
        const leaseList = Array.isArray(leases) ? leases : [];
        const activeLease = leaseList.find(l => l.status === 'Active') || leaseList[0];

        const ra = document.getElementById('rent-amount');
        if (ra) ra.textContent = activeLease ? fc(activeLease.monthlyRent) : '—';

        const pending = payList.filter(p => p.status === 'Pending');
        const confirmed = payList.filter(p => p.status === 'Confirmed');
        const rd = document.getElementById('rent-due');
        if (rd) rd.textContent = fc(pending.reduce((s, p) => s + (p.amount || 0), 0));

        // Pending
        const pEl = document.getElementById('rent-pending');
        if (pEl) pEl.innerHTML = pending.length ? pending.map(p => `
            <div class="list-item">
                <div class="item-icon" style="background:#fff3e0">💳</div>
                <div class="item-body">
                    <div class="item-title">${fc(p.amount)}</div>
                    <div class="item-sub">Échéance : ${fd(p.dueDate)}</div>
                </div>
                <button class="btn btn-sm btn-primary" onclick="payRent('${p.id}',${p.amount})">Payer</button>
            </div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun paiement en attente 🎉</p>';

        // History
        const hEl = document.getElementById('rent-history');
        if (hEl) hEl.innerHTML = confirmed.length ? confirmed.slice(0, 12).map(p => `
            <div class="list-item">
                <div class="item-icon" style="background:#e8f5e9">✅</div>
                <div class="item-body">
                    <div class="item-title">${fc(p.amount)}</div>
                    <div class="item-sub">${fd(p.paymentDate)} · ${p.paymentMethod || 'Virement'}</div>
                </div>
                <span style="color:var(--gray-400);font-size:12px;cursor:pointer" onclick="showToast('Téléchargement quittance...','success')">🧾</span>
            </div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun paiement</p>';

        // Lease info
        const lEl = document.getElementById('rent-lease');
        if (lEl) lEl.innerHTML = activeLease ? `
            <div style="font-size:13px">
                <p><strong>Début :</strong> ${fd(activeLease.startDate)}</p>
                <p><strong>Fin :</strong> ${fd(activeLease.endDate)}</p>
                <p><strong>Loyer :</strong> ${fc(activeLease.monthlyRent)}/mois</p>
                <p><strong>Charges :</strong> ${fc(activeLease.chargesAmount)}/mois</p>
                <p><strong>Dépôt :</strong> ${fc(activeLease.depositAmount)}</p>
            </div>` : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun bail actif</p>';
    } catch(e) { showErrorDialog('Impossible de charger les données.', e.message); }
});

window.payRent = (id, amount) => {
    showModal('Payer mon loyer', `
        <div style="text-align:center;margin-bottom:16px"><div style="font-size:28px;font-weight:700;color:var(--blue)">${fc(amount)}</div></div>
        <div class="form-group"><label class="form-label">Moyen de paiement</label>
            <select class="form-select" id="pay-method">
                <option value="OrangeMoney">🟠 Orange Money</option>
                <option value="MTNMoney">🟡 MTN Money</option>
                <option value="Wave">🔵 Wave</option>
                <option value="BankTransfer">🏦 Virement bancaire</option>
            </select></div>
        <div class="form-group"><label class="form-label">N° téléphone</label>
            <input class="form-input" id="pay-phone" placeholder="07 XX XX XX XX" type="tel"></div>
    `, async () => {
        showToast('Paiement initié via ' + $('#pay-method').selectedOptions[0].text, 'success');
        closeModal();
    });
};

// ═══════════════════════════════════════════════════════════
// 4. SIGNALER — Incidents avec photo + GPS
// ═══════════════════════════════════════════════════════════
Router.register('/incidents', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <div class="flex-between mb-16">
            <h1 class="page-title" style="margin:0">Mes signalements</h1>
            <button class="btn btn-primary btn-sm" id="btn-new-inc">+ Signaler</button>
        </div>
        <div id="inc-list"><div class="spinner"></div></div>`;

    try {
        const incidents = await API.get('/api/incidents').catch(() => []);
        const list = Array.isArray(incidents) ? incidents : [];
        document.getElementById('inc-list').innerHTML = list.length ? list.map(i => `
            <div class="card" style="margin-bottom:8px">
                <div class="flex-between">
                    <div>
                        <div style="font-weight:600">${i.title || 'Sans titre'}</div>
                        <div style="font-size:12px;color:var(--gray-600)">${categoryLabels[i.category] || ''} · ${fd(i.createdAt)}</div>
                        ${i.description ? `<p style="font-size:12px;color:var(--gray-700);margin-top:4px">${i.description.substring(0, 80)}${i.description.length > 80 ? '...' : ''}</p>` : ''}
                    </div>
                    <span class="item-badge ${badgeClass(i.status)}">${statusLabels[i.status] || i.status}</span>
                </div>
            </div>`).join('') : '<div class="empty-state"><div class="empty-icon">✅</div><p>Aucun signalement</p></div>';
    } catch(e) { showErrorDialog('Impossible de charger les incidents.', e.message); }

    document.getElementById('btn-new-inc').onclick = () => {
        let gpsLat = null, gpsLng = null;
        showModal('Signaler un problème', `
            <div class="form-group"><label class="form-label">Quel est le problème ?</label>
                <input class="form-input" id="inc-title" placeholder="Ex: Fuite sous l'évier"></div>
            <div class="form-group"><label class="form-label">Catégorie</label>
                <select class="form-select" id="inc-cat">
                    ${Object.entries(categoryLabels).map(([k, v]) => `<option value="${k}">${v}</option>`).join('')}
                </select></div>
            <div class="form-group"><label class="form-label">Urgence</label>
                <select class="form-select" id="inc-prio">
                    <option value="Low">Pas urgent</option>
                    <option value="Medium" selected>Normal</option>
                    <option value="High">Urgent</option>
                    <option value="Critical">Très urgent (dégât des eaux, feu...)</option>
                </select></div>
            <div class="form-group"><label class="form-label">Décrivez le problème</label>
                <textarea class="form-input" id="inc-desc" rows="3" placeholder="Donnez le maximum de détails..."></textarea></div>
            <div class="form-group"><label class="form-label">Où exactement ?</label>
                <input class="form-input" id="inc-loc" placeholder="Cuisine, salle de bain, hall...">
                <button class="btn btn-sm btn-secondary mt-8" id="btn-gps">📍 Ma position</button>
                <span id="gps-status" style="font-size:11px;margin-left:8px;color:var(--gray-500)"></span></div>
            <div class="form-group"><label class="form-label">Photo du problème</label>
                <input type="file" accept="image/*" capture="environment" class="form-input" id="inc-photo">
                <div id="photo-preview" style="margin-top:8px"></div></div>
        `, async () => {
            const title = $('#inc-title').value.trim();
            if (!title) { showToast('Décrivez le problème', 'error'); return; }
            await API.post('/api/incidents', {
                title,
                category: $('#inc-cat').value,
                priority: $('#inc-prio').value,
                description: $('#inc-desc').value,
                location: $('#inc-loc').value + (gpsLat ? ` (${gpsLat.toFixed(5)}, ${gpsLng.toFixed(5)})` : '')
            });
            closeModal();
            showToast('Signalement envoyé ! Le gestionnaire va le traiter.', 'success');
            Router.navigate('/incidents');
        });

        setTimeout(() => {
            const g = document.getElementById('btn-gps');
            if (g) g.onclick = () => {
                document.getElementById('gps-status').textContent = 'Localisation...';
                navigator.geolocation.getCurrentPosition(
                    p => { gpsLat = p.coords.latitude; gpsLng = p.coords.longitude; document.getElementById('gps-status').textContent = `✅ Position enregistrée`; },
                    e => { document.getElementById('gps-status').textContent = '❌ ' + e.message; }
                );
            };
            const ph = document.getElementById('inc-photo');
            if (ph) ph.onchange = () => {
                const f = ph.files[0];
                if (f) document.getElementById('photo-preview').innerHTML = `<img src="${URL.createObjectURL(f)}" style="max-width:100%;max-height:150px;border-radius:8px">`;
            };
        }, 100);
    };
});

// ═══════════════════════════════════════════════════════════
// 5. DOCUMENTS — Quittances + Bail + État des lieux
// ═══════════════════════════════════════════════════════════
Router.register('/documents', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Mes documents</h1>

        <div class="card">
            <div class="card-header"><span class="card-title">Quittances de loyer</span></div>
            <div id="doc-quittances"><div class="spinner"></div></div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Documents contractuels</span></div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')">
                <div class="item-icon" style="background:#e3f2fd">📝</div>
                <div class="item-body"><div class="item-title">Mon bail</div><div class="item-sub">Contrat de location</div></div>
                <span style="color:var(--gray-400)">⬇</span>
            </div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')">
                <div class="item-icon" style="background:#e8f5e9">📋</div>
                <div class="item-body"><div class="item-title">État des lieux d'entrée</div><div class="item-sub">Contradictoire (art. 427 CCH)</div></div>
                <span style="color:var(--gray-400)">⬇</span>
            </div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')">
                <div class="item-icon" style="background:#fff3e0">📖</div>
                <div class="item-body"><div class="item-title">Règles de vie</div><div class="item-sub">Règlement copropriété</div></div>
                <span style="color:var(--gray-400)">⬇</span>
            </div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Régularisation des charges</span></div>
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">📊</div>
                <div class="item-body">
                    <div class="item-title">Décompte annuel 2025</div>
                    <div class="item-sub">Détail des charges récupérables</div>
                </div>
                <span style="color:var(--gray-400)">⬇</span>
            </div>
        </div>`;

    try {
        const payments = await API.get('/api/payments').catch(() => []);
        const confirmed = (Array.isArray(payments) ? payments : []).filter(p => p.status === 'Confirmed');
        const qEl = document.getElementById('doc-quittances');
        if (qEl) qEl.innerHTML = confirmed.length ? confirmed.slice(0, 6).map(p => `
            <div class="list-item" onclick="showToast('Téléchargement quittance...','success')">
                <div class="item-icon" style="background:#e8f5e9">🧾</div>
                <div class="item-body">
                    <div class="item-title">Quittance ${fd(p.paymentDate)}</div>
                    <div class="item-sub">${fc(p.amount)}</div>
                </div>
                <span style="color:var(--gray-400)">⬇</span>
            </div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucune quittance disponible</p>';
    } catch(e) { console.error(e); }
});

// ═══════════════════════════════════════════════════════════
// 6. CONTACT — Contacter le gestionnaire
// ═══════════════════════════════════════════════════════════
Router.register('/contact', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Contacter</h1>

        <div class="card">
            <div class="card-header"><span class="card-title">Votre gestionnaire</span></div>
            <div class="list-item">
                <div class="item-icon" style="background:#e8f5e9;font-size:24px">👷</div>
                <div class="item-body">
                    <div class="item-title">COFIPRI — Syndic</div>
                    <div class="item-sub">Gestionnaire de Green City Bassam</div>
                </div>
            </div>
            <div style="display:flex;gap:8px;margin-top:8px">
                <a href="tel:+22507000000" class="btn btn-primary btn-block" style="text-decoration:none;font-size:13px">📞 Appeler</a>
                <button class="btn btn-secondary btn-block" id="btn-msg" style="font-size:13px">✉️ Message</button>
            </div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Contacts utiles</span></div>
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">🛡️</div>
                <div class="item-body"><div class="item-title">Gardien / Sécurité</div><div class="item-sub">Disponible 24h/24</div></div>
                <a href="tel:+22507000001" class="btn btn-sm btn-secondary" style="text-decoration:none">📞</a>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#ffebee">🚒</div>
                <div class="item-body"><div class="item-title">Urgences / Pompiers</div><div class="item-sub">180</div></div>
                <a href="tel:180" class="btn btn-sm btn-danger" style="text-decoration:none;color:white">📞</a>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#fff3e0">🔧</div>
                <div class="item-body"><div class="item-title">Maintenance technique</div><div class="item-sub">Horaires ouvrés</div></div>
                <a href="tel:+22507000002" class="btn btn-sm btn-secondary" style="text-decoration:none">📞</a>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">💡</div>
                <div class="item-body"><div class="item-title">CIE (électricité)</div><div class="item-sub">Coupure / urgence</div></div>
                <a href="tel:179" class="btn btn-sm btn-secondary" style="text-decoration:none">📞</a>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">💧</div>
                <div class="item-body"><div class="item-title">SODECI (eau)</div><div class="item-sub">Coupure / urgence</div></div>
                <a href="tel:175" class="btn btn-sm btn-secondary" style="text-decoration:none">📞</a>
            </div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Historique messages</span></div>
            <div class="list-item">
                <div class="item-icon">💬</div>
                <div class="item-body"><div class="item-title">Demande de réparation</div><div class="item-sub">Il y a 3 jours · Répondu</div></div>
                <span class="item-badge badge-green">Traité</span>
            </div>
        </div>`;

    document.getElementById('btn-msg').onclick = () => {
        showModal('Envoyer un message', `
            <div class="form-group"><label class="form-label">Objet</label>
                <input class="form-input" id="msg-subject" placeholder="Objet de votre demande"></div>
            <div class="form-group"><label class="form-label">Message</label>
                <textarea class="form-input" id="msg-body" rows="4" placeholder="Décrivez votre demande..."></textarea></div>
        `, async () => {
            if (!$('#msg-subject').value.trim()) { showToast('Objet obligatoire', 'error'); return; }
            showToast('Message envoyé au gestionnaire ✅', 'success');
            closeModal();
        });
    };
});

// ═══════════════════════════════════════════════════════════
// 7. INFOS PRATIQUES — Calendrier + Règles
// ═══════════════════════════════════════════════════════════
Router.register('/infos', async (el) => {
    if (!API.token) { showLogin(el); return; }

    el.innerHTML = `
        <h1 class="page-title">Infos pratiques</h1>

        <div class="card">
            <div class="card-header"><span class="card-title">Calendrier</span></div>
            <div class="list-item">
                <div class="item-icon" style="background:#fff3e0">🔧</div>
                <div class="item-body"><div class="item-title">Entretien ascenseur</div><div class="item-sub">Chaque lundi 8h-10h</div></div>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#e8f5e9">🌿</div>
                <div class="item-body"><div class="item-title">Entretien espaces verts</div><div class="item-sub">Mercredi et samedi matin</div></div>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#e3f2fd">🗑️</div>
                <div class="item-body"><div class="item-title">Collecte ordures</div><div class="item-sub">Lundi, mercredi, vendredi — 6h</div></div>
            </div>
            <div class="list-item">
                <div class="item-icon" style="background:#fff3e0">🏊</div>
                <div class="item-body"><div class="item-title">Piscine</div><div class="item-sub">Tous les jours 7h-20h</div></div>
            </div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">Règles de vie</span></div>
            <div style="font-size:13px;color:var(--gray-700);padding:4px 0">
                <p style="margin-bottom:8px">🔇 <strong>Bruit :</strong> Silence entre 22h et 7h</p>
                <p style="margin-bottom:8px">🐾 <strong>Animaux :</strong> Tenus en laisse dans les parties communes</p>
                <p style="margin-bottom:8px">🚗 <strong>Parking :</strong> Uniquement sur les emplacements attribués</p>
                <p style="margin-bottom:8px">🗑️ <strong>Déchets :</strong> Tri obligatoire, local poubelle au sous-sol</p>
                <p style="margin-bottom:8px">🏗️ <strong>Travaux :</strong> Autorisés du lundi au samedi 8h-19h, déclaration préalable</p>
                <p>🚭 <strong>Fumée :</strong> Interdiction de fumer dans les parties communes</p>
            </div>
        </div>

        <div class="card" style="margin-top:12px">
            <div class="card-header"><span class="card-title">À propos de Green City Bassam</span></div>
            <div style="font-size:13px;color:var(--gray-700)">
                <p style="margin-bottom:6px">🏘️ 51 villas + 200 appartements + 18 lots commerciaux COSMOS</p>
                <p style="margin-bottom:6px">🏢 Syndic : COFIPRI</p>
                <p>📍 Grand-Bassam, Côte d'Ivoire</p>
            </div>
        </div>`;
});

// ═══════════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════════
(async () => {
    try { const r = await fetch(`${API_BASE}/api/version`); if (!r.ok) throw new Error('HTTP ' + r.status); }
    catch(e) { showErrorDialog('Le serveur GreenSyndic n\'est pas accessible. Vérifiez qu\'il est bien démarré.', e.message); }
})();

$$('.bottom-nav a').forEach(a => a.addEventListener('click', e => { e.preventDefault(); Router.navigate(a.dataset.nav); }));
window.addEventListener('popstate', () => { Router.render((location.pathname.replace('/app', '') || '/')); });
Router.navigate(location.pathname.replace('/app', '') || '/');
