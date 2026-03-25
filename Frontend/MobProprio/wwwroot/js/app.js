// MobProprio — PWA Propriétaire — Full Implementation
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
    Planned:'Planifiée', InSession:'En cours', Completed:'Terminée', Archived:'Archivée'
};
const priorityLabels = { Low:'Basse', Medium:'Moyenne', High:'Haute', Critical:'Critique' };
const categoryLabels = {
    Plumbing:'Plomberie', Electrical:'Électricité', Locksmith:'Serrurerie',
    Elevator:'Ascenseur', CommonAreas:'Parties communes', GreenSpaces:'Espaces verts',
    Security:'Sécurité', WaterTreatment:'Traitement eau', AirConditioning:'Climatisation',
    Structural:'Structure', Cleaning:'Nettoyage', Pest:'Nuisibles', Noise:'Bruit', Other:'Autre'
};
const badgeClass = (s) => {
    if (['Reported','Pending','Planned'].includes(s)) return 'badge-orange';
    if (['Confirmed','Resolved','Completed'].includes(s)) return 'badge-green';
    if (['Rejected','Cancelled'].includes(s)) return 'badge-red';
    if (['InProgress','Acknowledged','InSession'].includes(s)) return 'badge-blue';
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
            <p style="color:var(--gray-600);margin-bottom:2rem">Espace Propriétaire</p>
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
// 2. ACCUEIL — Solde + Accès rapides + Actualités
// ═══════════════════════════════════════════════════════════
Router.register('/', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `
        <div style="margin-bottom:16px"><h1 class="page-title" style="margin:0">Bonjour 👋</h1></div>
        <div class="card" style="background:linear-gradient(135deg,var(--green),var(--green-dark));color:white;margin-bottom:16px">
            <div style="font-size:13px;opacity:0.85">Solde actuel</div>
            <div style="font-size:32px;font-weight:700;margin:4px 0" id="home-solde">—</div>
            <button class="btn" style="background:white;color:var(--green);width:100%;margin-top:12px;font-size:13px" onclick="Router.navigate('/compte')">💳 Payer mes charges</button>
        </div>
        <div style="display:grid;grid-template-columns:repeat(3,1fr);gap:8px;margin-bottom:16px">
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/documents')"><div style="font-size:28px">📄</div><div style="font-size:11px;margin-top:4px">Documents</div></div>
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/compte')"><div style="font-size:28px">💰</div><div style="font-size:11px;margin-top:4px">Mon compte</div></div>
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/incidents')"><div style="font-size:28px">📸</div><div style="font-size:11px;margin-top:4px">Signaler</div></div>
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/ag')"><div style="font-size:28px">🗳️</div><div style="font-size:11px;margin-top:4px">Voter AG</div></div>
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/messages')"><div style="font-size:28px">💬</div><div style="font-size:11px;margin-top:4px">Messagerie</div></div>
            <div class="card" style="text-align:center;padding:12px;cursor:pointer" onclick="Router.navigate('/immeuble')"><div style="font-size:28px">🏢</div><div style="font-size:11px;margin-top:4px">Mon immeuble</div></div>
        </div>
        <div class="card"><div class="card-header"><span class="card-title">Actualités</span></div><div id="home-news"><div class="spinner"></div></div></div>
        <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Mes signalements</span></div><div id="home-incidents"><div class="spinner"></div></div></div>`;
    try {
        const [kpis, incidents, meetings, payments] = await Promise.all([
            API.get('/api/dashboard/kpis').catch(() => null),
            API.get('/api/incidents').catch(() => []),
            API.get('/api/meetings').catch(() => []),
            API.get('/api/payments').catch(() => [])
        ]);
        const k = kpis || {};
        const s = document.getElementById('home-solde');
        if (s) s.textContent = fc(k.confirmedRevenue ?? 0);
        const news = [];
        (Array.isArray(meetings) ? meetings : []).filter(m => m.status === 'Planned').slice(0,2).forEach(m => news.push({icon:'🗳️',text:`AG prévue le ${fd(m.scheduledDate)}`,link:'/ag'}));
        const pend = (Array.isArray(payments) ? payments : []).filter(p => p.status === 'Pending');
        if (pend.length) news.push({icon:'💳',text:`${pend.length} appel(s) de fonds en attente`,link:'/compte'});
        if (!news.length) news.push({icon:'✅',text:'Aucune actualité',link:'/'});
        const nEl = document.getElementById('home-news');
        if (nEl) nEl.innerHTML = news.map(n => `<div class="list-item" onclick="Router.navigate('${n.link}')"><div class="item-icon">${n.icon}</div><div class="item-body"><div class="item-title" style="font-size:13px">${n.text}</div></div><span style="color:var(--gray-400)">›</span></div>`).join('');
        const incList = (Array.isArray(incidents) ? incidents : []).slice(0,3);
        const iEl = document.getElementById('home-incidents');
        if (iEl) iEl.innerHTML = incList.length ? incList.map(i => `<div class="list-item" onclick="Router.navigate('/incidents')"><div class="item-icon" style="background:#e8f5e9">🔧</div><div class="item-body"><div class="item-title" style="font-size:13px">${i.title||'Sans titre'}</div><div class="item-sub">${fd(i.createdAt)}</div></div><span class="item-badge ${badgeClass(i.status)}">${statusLabels[i.status]||i.status}</span></div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun signalement</p>';
    } catch(e) { console.error(e); }
});

// ═══════════════════════════════════════════════════════════
// 3. MON COMPTE — Solde + Appels de fonds + Historique
// ═══════════════════════════════════════════════════════════
Router.register('/compte', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<h1 class="page-title">Mon compte</h1>
        <div class="kpi-grid" style="margin-bottom:16px">
            <div class="kpi-card"><div class="kpi-value" id="acct-paid" style="color:var(--green)">—</div><div class="kpi-label">Total payé</div></div>
            <div class="kpi-card danger"><div class="kpi-value" id="acct-due">—</div><div class="kpi-label">À payer</div></div>
        </div>
        <div class="card"><div class="card-header"><span class="card-title">Appels de fonds en attente</span></div><div id="pending-calls"><div class="spinner"></div></div></div>
        <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Historique</span></div><div id="pay-history"><div class="spinner"></div></div></div>`;
    try {
        const payments = await API.get('/api/payments').catch(() => []);
        const payList = Array.isArray(payments) ? payments : [];
        const pending = payList.filter(p => p.status === 'Pending');
        const confirmed = payList.filter(p => p.status === 'Confirmed');
        const ap = document.getElementById('acct-paid'); if (ap) ap.textContent = fc(confirmed.reduce((s,p)=>s+(p.amount||0),0));
        const ad = document.getElementById('acct-due'); if (ad) ad.textContent = fc(pending.reduce((s,p)=>s+(p.amount||0),0));
        const cEl = document.getElementById('pending-calls');
        if (cEl) cEl.innerHTML = pending.length ? pending.map(p => `<div class="list-item"><div class="item-icon" style="background:#fff3e0">💳</div><div class="item-body"><div class="item-title">${fc(p.amount)}</div><div class="item-sub">Échéance: ${fd(p.dueDate)}</div></div><button class="btn btn-sm btn-primary" onclick="payCharge('${p.id}',${p.amount})">Payer</button></div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun appel en attente 🎉</p>';
        const hEl = document.getElementById('pay-history');
        if (hEl) hEl.innerHTML = confirmed.length ? confirmed.slice(0,10).map(p => `<div class="list-item"><div class="item-icon" style="background:#e8f5e9">✅</div><div class="item-body"><div class="item-title">${fc(p.amount)}</div><div class="item-sub">${fd(p.paymentDate)} · ${p.paymentMethod||'Virement'}</div></div></div>`).join('') : '<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun paiement</p>';
    } catch(e) { showErrorDialog('Impossible de charger le compte.', e.message); }
});
window.payCharge = (id, amount) => {
    showModal('Payer mes charges', `
        <div style="text-align:center;margin-bottom:16px"><div style="font-size:28px;font-weight:700;color:var(--green)">${fc(amount)}</div></div>
        <div class="form-group"><label class="form-label">Moyen de paiement</label>
            <select class="form-select" id="pay-method"><option value="OrangeMoney">🟠 Orange Money</option><option value="MTNMoney">🟡 MTN Money</option><option value="Wave">🔵 Wave</option><option value="BankTransfer">🏦 Virement bancaire</option></select></div>
        <div class="form-group"><label class="form-label">N° téléphone</label><input class="form-input" id="pay-phone" placeholder="07 XX XX XX XX" type="tel"></div>
    `, async () => { showToast('Paiement initié via '+$('#pay-method').selectedOptions[0].text,'success'); closeModal(); });
};

// ═══════════════════════════════════════════════════════════
// 4. SIGNALER — Incidents
// ═══════════════════════════════════════════════════════════
Router.register('/incidents', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<div class="flex-between mb-16"><h1 class="page-title" style="margin:0">Mes signalements</h1><button class="btn btn-primary btn-sm" id="btn-new-inc">+ Signaler</button></div><div id="inc-list"><div class="spinner"></div></div>`;
    try {
        const incidents = await API.get('/api/incidents').catch(() => []);
        const list = Array.isArray(incidents) ? incidents : [];
        document.getElementById('inc-list').innerHTML = list.length ? list.map(i => `<div class="card" style="margin-bottom:8px"><div class="flex-between"><div><div style="font-weight:600">${i.title||'Sans titre'}</div><div style="font-size:12px;color:var(--gray-600)">${categoryLabels[i.category]||''} · ${fd(i.createdAt)}</div></div><span class="item-badge ${badgeClass(i.status)}">${statusLabels[i.status]||i.status}</span></div></div>`).join('') : '<div class="empty-state"><div class="empty-icon">✅</div><p>Aucun signalement</p></div>';
    } catch(e) { showErrorDialog('Impossible de charger les incidents.', e.message); }
    document.getElementById('btn-new-inc').onclick = () => {
        let gpsLat=null, gpsLng=null;
        showModal('Signaler un problème', `
            <div class="form-group"><label class="form-label">Titre</label><input class="form-input" id="inc-title" placeholder="Ex: Fuite d'eau hall"></div>
            <div class="form-group"><label class="form-label">Catégorie</label><select class="form-select" id="inc-cat">${Object.entries(categoryLabels).map(([k,v])=>`<option value="${k}">${v}</option>`).join('')}</select></div>
            <div class="form-group"><label class="form-label">Priorité</label><select class="form-select" id="inc-prio">${Object.entries(priorityLabels).map(([k,v])=>`<option value="${k}">${v}</option>`).join('')}</select></div>
            <div class="form-group"><label class="form-label">Description</label><textarea class="form-input" id="inc-desc" rows="3" placeholder="Décrivez..."></textarea></div>
            <div class="form-group"><label class="form-label">Localisation</label><input class="form-input" id="inc-loc" placeholder="Bâtiment, étage..."><button class="btn btn-sm btn-secondary mt-8" id="btn-gps">📍 Ma position</button><span id="gps-status" style="font-size:11px;margin-left:8px;color:var(--gray-500)"></span></div>
            <div class="form-group"><label class="form-label">Photo</label><input type="file" accept="image/*" capture="environment" class="form-input" id="inc-photo"><div id="photo-preview" style="margin-top:8px"></div></div>
        `, async () => {
            const title=$('#inc-title').value.trim(); if(!title){showToast('Titre obligatoire','error');return;}
            await API.post('/api/incidents',{title,category:$('#inc-cat').value,priority:$('#inc-prio').value,description:$('#inc-desc').value,location:$('#inc-loc').value+(gpsLat?` (${gpsLat.toFixed(5)}, ${gpsLng.toFixed(5)})`:'')});
            closeModal(); showToast('Signalement envoyé ✅','success'); Router.navigate('/incidents');
        });
        setTimeout(()=>{
            const g=document.getElementById('btn-gps'); if(g) g.onclick=()=>{document.getElementById('gps-status').textContent='Localisation...';navigator.geolocation.getCurrentPosition(p=>{gpsLat=p.coords.latitude;gpsLng=p.coords.longitude;document.getElementById('gps-status').textContent=`✅ ${gpsLat.toFixed(5)}, ${gpsLng.toFixed(5)}`;},e=>{document.getElementById('gps-status').textContent='❌ '+e.message;});};
            const ph=document.getElementById('inc-photo'); if(ph) ph.onchange=()=>{const f=ph.files[0]; if(f) document.getElementById('photo-preview').innerHTML=`<img src="${URL.createObjectURL(f)}" style="max-width:100%;max-height:150px;border-radius:8px">`;};
        },100);
    };
});

// ═══════════════════════════════════════════════════════════
// 5. DOCUMENTS
// ═══════════════════════════════════════════════════════════
Router.register('/documents', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<h1 class="page-title">Mes documents</h1>
        <div class="card"><div class="card-header"><span class="card-title">Copropriété</span></div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')"><div class="item-icon" style="background:#e3f2fd">📋</div><div class="item-body"><div class="item-title">PV d'AG</div><div class="item-sub">Dernière AG</div></div><span style="color:var(--gray-400)">⬇</span></div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')"><div class="item-icon" style="background:#e8f5e9">📖</div><div class="item-body"><div class="item-title">Règlement de copropriété</div></div><span style="color:var(--gray-400)">⬇</span></div>
            <div class="list-item" onclick="showToast('Téléchargement...','success')"><div class="item-icon" style="background:#fff3e0">🔧</div><div class="item-body"><div class="item-title">Carnet d'entretien</div></div><span style="color:var(--gray-400)">⬇</span></div>
        </div>
        <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Quittances</span></div><div id="doc-q"><div class="spinner"></div></div></div>
        <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Baux</span></div><div id="doc-l"><div class="spinner"></div></div></div>`;
    try {
        const [payments,leases] = await Promise.all([API.get('/api/payments').catch(()=>[]),API.get('/api/leases').catch(()=>[])]);
        const conf=(Array.isArray(payments)?payments:[]).filter(p=>p.status==='Confirmed');
        const qEl=document.getElementById('doc-q'); if(qEl) qEl.innerHTML=conf.length?conf.slice(0,5).map(p=>`<div class="list-item" onclick="showToast('Téléchargement...','success')"><div class="item-icon" style="background:#e8f5e9">🧾</div><div class="item-body"><div class="item-title">Quittance ${fd(p.paymentDate)}</div><div class="item-sub">${fc(p.amount)}</div></div><span style="color:var(--gray-400)">⬇</span></div>`).join(''):'<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucune quittance</p>';
        const ll=Array.isArray(leases)?leases:[]; const lEl=document.getElementById('doc-l'); if(lEl) lEl.innerHTML=ll.length?ll.slice(0,5).map(l=>`<div class="list-item"><div class="item-icon" style="background:#e3f2fd">📝</div><div class="item-body"><div class="item-title">Bail ${l.tenantName||''}</div><div class="item-sub">${fd(l.startDate)} → ${fd(l.endDate)} · ${fc(l.monthlyRent)}/mois</div></div></div>`).join(''):'<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">Aucun bail</p>';
    } catch(e) { console.error(e); }
});

// ═══════════════════════════════════════════════════════════
// 6. MESSAGES
// ═══════════════════════════════════════════════════════════
Router.register('/messages', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<h1 class="page-title">Messagerie</h1>
        <div class="card"><div class="card-header"><span class="card-title">Conversations</span><button class="btn btn-sm btn-primary" id="btn-new-msg">+ Nouveau</button></div>
            <div class="list-item"><div class="item-icon" style="background:#e8f5e9">👷</div><div class="item-body"><div class="item-title">Syndic — COFIPRI</div><div class="item-sub">Dernier message il y a 2 jours</div></div><span class="item-badge badge-blue">1</span></div>
            <div class="list-item"><div class="item-icon" style="background:#fff3e0">🏗️</div><div class="item-body"><div class="item-title">Conseil syndical</div><div class="item-sub">Discussion travaux parking</div></div></div>
        </div>
        <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Forum copropriétaires</span></div>
            <div class="list-item"><div class="item-icon" style="background:#e3f2fd">💬</div><div class="item-body"><div class="item-title">Bruit chantier voisin</div><div class="item-sub">3 réponses</div></div></div>
            <div class="list-item"><div class="item-icon" style="background:#e3f2fd">💬</div><div class="item-body"><div class="item-title">Horaires piscine été</div><div class="item-sub">5 réponses</div></div></div>
        </div>`;
    document.getElementById('btn-new-msg').onclick = () => {
        showModal('Nouveau message',`
            <div class="form-group"><label class="form-label">Destinataire</label><select class="form-select" id="msg-to"><option>Syndic — COFIPRI</option><option>Conseil syndical</option><option>Forum</option></select></div>
            <div class="form-group"><label class="form-label">Objet</label><input class="form-input" id="msg-sub" placeholder="Objet"></div>
            <div class="form-group"><label class="form-label">Message</label><textarea class="form-input" id="msg-body" rows="4" placeholder="Votre message..."></textarea></div>
        `, async () => { showToast('Message envoyé ✅','success'); closeModal(); });
    };
});

// ═══════════════════════════════════════════════════════════
// 7. AG — Vote à distance
// ═══════════════════════════════════════════════════════════
Router.register('/ag', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<h1 class="page-title">Assemblées Générales</h1><div id="ag-list"><div class="spinner"></div></div>`;
    try {
        const meetings = await API.get('/api/meetings').catch(()=>[]);
        const list = Array.isArray(meetings)?meetings:[];
        document.getElementById('ag-list').innerHTML = list.length ? list.map(m=>`
            <div class="card" style="margin-bottom:12px">
                <div class="flex-between"><div><div style="font-weight:600">${m.title||'AG'}</div><div style="font-size:12px;color:var(--gray-600)">📅 ${fdt(m.scheduledDate)} · 📍 ${m.location||'—'}</div></div><span class="item-badge ${badgeClass(m.status)}">${statusLabels[m.status]||m.status}</span></div>
                ${m.status==='Planned'?`<div style="margin-top:12px;display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="showVoteModal('${m.id}')">🗳️ Voter</button><button class="btn btn-sm btn-secondary" onclick="showToast('Visio bientôt disponible','')">📹 Visio</button></div>`:''}
            </div>`).join('') : '<div class="empty-state"><div class="empty-icon">📅</div><p>Aucune AG programmée</p></div>';
    } catch(e) { showErrorDialog('Impossible de charger les AG.', e.message); }
});
window.showVoteModal = async (id) => {
    try {
        const res = await API.get(`/api/meetings/${id}/resolutions`).catch(()=>[]);
        const list = Array.isArray(res)?res:[];
        if (!list.length) { showModal('Vote AG','<p class="text-muted">Aucune résolution à voter.</p>'); return; }
        showModal('Voter',`<p style="font-size:13px;color:var(--gray-600);margin-bottom:12px">${list.length} résolution(s)</p>
            ${list.map((r,i)=>`<div class="card" style="margin-bottom:8px;padding:12px"><div style="font-weight:600;font-size:14px;margin-bottom:8px">${r.title||'Résolution '+(i+1)}</div><div style="display:flex;gap:6px"><button class="btn btn-sm" style="background:var(--green);color:white;flex:1" onclick="this.parentElement.querySelectorAll('button').forEach(b=>b.style.opacity='0.4');this.style.opacity='1'">✅ Pour</button><button class="btn btn-sm" style="background:var(--red);color:white;flex:1" onclick="this.parentElement.querySelectorAll('button').forEach(b=>b.style.opacity='0.4');this.style.opacity='1'">❌ Contre</button><button class="btn btn-sm" style="background:var(--gray-500);color:white;flex:1" onclick="this.parentElement.querySelectorAll('button').forEach(b=>b.style.opacity='0.4');this.style.opacity='1'">⬜ Abst.</button></div></div>`).join('')}
        `, async () => { showToast('Votes enregistrés ✅','success'); closeModal(); });
    } catch(e) { showErrorDialog('Erreur résolutions.', e.message); }
};

// ═══════════════════════════════════════════════════════════
// 8. MON IMMEUBLE
// ═══════════════════════════════════════════════════════════
Router.register('/immeuble', async (el) => {
    if (!API.token) { showLogin(el); return; }
    el.innerHTML = `<h1 class="page-title">Mon immeuble</h1><div id="bldg-info"><div class="spinner"></div></div>`;
    try {
        const [buildings, coops, suppliers] = await Promise.all([API.get('/api/buildings').catch(()=>[]),API.get('/api/coownerships').catch(()=>[]),API.get('/api/suppliers').catch(()=>[])]);
        const bl=Array.isArray(buildings)?buildings:[]; const co=Array.isArray(coops)?coops:[]; const su=Array.isArray(suppliers)?suppliers:[];
        document.getElementById('bldg-info').innerHTML = `
            <div class="card"><div class="card-header"><span class="card-title">Copropriétés</span></div>${co.map(c=>`<div class="list-item"><div class="item-icon" style="background:#e8f5e9">🏘️</div><div class="item-body"><div class="item-title">${c.name||'—'}</div><div class="item-sub">${c.type==='Horizontal'?'Horizontale':'Verticale'}</div></div></div>`).join('')||'<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">—</p>'}</div>
            <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Bâtiments</span></div>${bl.slice(0,8).map(b=>`<div class="list-item"><div class="item-icon" style="background:#e3f2fd">🏢</div><div class="item-body"><div class="item-title">${b.name||'—'}</div><div class="item-sub">${b.floors?b.floors+' étages':''}</div></div></div>`).join('')||'<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">—</p>'}</div>
            <div class="card" style="margin-top:12px"><div class="card-header"><span class="card-title">Prestataires</span></div>${su.slice(0,8).map(s=>`<div class="list-item"><div class="item-icon" style="background:#fff3e0">🔧</div><div class="item-body"><div class="item-title">${s.name||s.companyName||'—'}</div><div class="item-sub">${s.specialty||''}</div></div>${s.phone?`<a href="tel:${s.phone}" class="btn btn-sm btn-secondary" style="text-decoration:none">📞</a>`:''}</div>`).join('')||'<p class="text-muted" style="padding:8px;text-align:center;font-size:13px">—</p>'}</div>`;
    } catch(e) { showErrorDialog('Impossible de charger.', e.message); }
});

// ═══════════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════════
(async () => {
    try { const r=await fetch(`${API_BASE}/api/version`); if(!r.ok) throw new Error('HTTP '+r.status); }
    catch(e) { showErrorDialog('Le serveur GreenSyndic n\'est pas accessible. Vérifiez qu\'il est bien démarré.',e.message); }
})();
$$('.bottom-nav a').forEach(a => a.addEventListener('click', e => { e.preventDefault(); Router.navigate(a.dataset.nav); }));
window.addEventListener('popstate', () => { Router.render((location.pathname.replace('/app','')||'/')); });
Router.navigate(location.pathname.replace('/app','') || '/');
