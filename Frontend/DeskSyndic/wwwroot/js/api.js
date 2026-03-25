// DeskSyndic — API client → GreenSyndic.Api :5050
const API_URL = 'http://localhost:5050';

// ── Error dialog — ALWAYS a modal with Detail + Copier ──
function showErrorDialog(userMsg, technicalDetail) {
    document.getElementById('api-error-dialog')?.remove();
    const overlay = document.createElement('div');
    overlay.id = 'api-error-dialog';
    overlay.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.5);z-index:99999;display:flex;align-items:center;justify-content:center';
    overlay.innerHTML = `
        <div style="background:#fff;border-radius:12px;max-width:500px;width:90%;box-shadow:0 8px 32px rgba(0,0,0,.25);overflow:hidden">
            <div style="padding:24px">
                <div style="font-size:36px;text-align:center;margin-bottom:12px">\u26a0\ufe0f</div>
                <div style="font-weight:700;font-size:16px;text-align:center;margin-bottom:8px;color:#b91c1c">Erreur</div>
                <p style="color:#374151;text-align:center;margin:0 0 16px;font-size:14px">${userMsg}</p>
                <div id="err-detail-section" style="display:none;margin-top:12px">
                    <pre id="err-detail-text" style="background:#f3f4f6;padding:12px;border-radius:8px;font-size:11px;color:#6b7280;white-space:pre-wrap;word-break:break-all;max-height:200px;overflow-y:auto;margin:0"></pre>
                </div>
            </div>
            <div style="padding:12px 24px;border-top:1px solid #e5e7eb;display:flex;gap:8px;justify-content:space-between">
                <div>
                    <button id="err-detail-btn" style="background:none;border:1px solid #d1d5db;border-radius:6px;padding:6px 14px;font-size:12px;color:#6b7280;cursor:pointer">Detail</button>
                    <button id="err-copy-btn" style="display:none;background:none;border:1px solid #d1d5db;border-radius:6px;padding:6px 14px;font-size:12px;color:#6b7280;cursor:pointer;margin-left:4px">Copier</button>
                </div>
                <button id="err-close-btn" style="background:#2e7d32;color:#fff;border:none;border-radius:6px;padding:8px 24px;font-size:14px;font-weight:600;cursor:pointer">Fermer</button>
            </div>
        </div>`;
    document.body.appendChild(overlay);

    document.getElementById('err-close-btn').onclick = () => overlay.remove();
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };

    const detailSection = document.getElementById('err-detail-section');
    const detailText = document.getElementById('err-detail-text');
    const detailBtn = document.getElementById('err-detail-btn');
    const copyBtn = document.getElementById('err-copy-btn');
    detailText.textContent = technicalDetail;

    detailBtn.onclick = () => {
        detailSection.style.display = 'block';
        copyBtn.style.display = 'inline-block';
        detailBtn.style.display = 'none';
    };
    copyBtn.onclick = () => {
        navigator.clipboard.writeText(technicalDetail).then(() => {
            copyBtn.textContent = 'Copie !';
            setTimeout(() => { copyBtn.textContent = 'Copier'; }, 2000);
        });
    };
}

const API = {
    base: API_URL + '/api',
    token: localStorage.getItem('gs_token'),

    async request(method, path, body) {
        const url = `${API_URL}/api${path}`;
        const opts = { method, headers: { 'Content-Type': 'application/json' } };
        if (this.token) opts.headers['Authorization'] = `Bearer ${this.token}`;
        if (body) opts.body = JSON.stringify(body);

        let r;
        try {
            opts.signal = AbortSignal.timeout(5000);
            r = await fetch(url, opts);
        } catch (e) {
            // Network error or timeout — backend unreachable
            const detail = `${method} ${url}\n\nErreur reseau: ${e.name}: ${e.message}\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Impossible de joindre le serveur GreenSyndic.\nVerifiez qu'il est bien demarre.",
                detail
            );
            return null;
        }

        // 401 = session expiree ou token invalide → rediriger vers login
        if (r.status === 401) {
            const detail = `${method} ${url}\n\nHTTP 401 Unauthorized\nToken: ${this.token ? this.token.substring(0, 20) + '...' : 'AUCUN'}\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Votre session a expire ou vos identifiants sont invalides.\nVeuillez vous reconnecter.",
                detail
            );
            this.token = null;
            localStorage.removeItem('gs_token');
            setTimeout(() => Router.navigate('/login'), 2000);
            return null;
        }

        // 403 = acces interdit
        if (r.status === 403) {
            const detail = `${method} ${url}\n\nHTTP 403 Forbidden\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Vous n'avez pas les droits pour effectuer cette action.",
                detail
            );
            return null;
        }

        // 404 = endpoint introuvable — retourner null sans modale (normal pour certaines pages)
        if (r.status === 404) return null;

        // Autre erreur HTTP
        if (!r.ok) {
            const errBody = await r.text().catch(() => '');
            const detail = `${method} ${url}\n\nHTTP ${r.status} ${r.statusText}\n\nReponse:\n${errBody}\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Un probleme est survenu. Veuillez reessayer.",
                detail
            );
            return null;
        }

        // Success
        const text = await r.text();
        return text ? JSON.parse(text) : null;
    },

    get: (p) => API.request('GET', p),
    post: (p, b) => API.request('POST', p, b),
    put: (p, b) => API.request('PUT', p, b),
    del: (p) => API.request('DELETE', p),

    async login(email, password) {
        const url = `${API_URL}/api/auth/login`;
        let r;
        try {
            r = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password }),
                signal: AbortSignal.timeout(5000)
            });
        } catch (e) {
            const detail = `POST ${url}\n\nErreur reseau: ${e.name}: ${e.message}\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Impossible de joindre le serveur GreenSyndic.\nVerifiez qu'il est bien demarre.",
                detail
            );
            throw e;
        }

        if (!r.ok) {
            const errBody = await r.text().catch(() => '');
            const detail = `POST ${url}\n\nHTTP ${r.status} ${r.statusText}\n\nReponse:\n${errBody}\n\nDate: ${new Date().toLocaleString('fr-CI')}`;
            showErrorDialog(
                "Identifiants invalides. Verifiez votre email et mot de passe.",
                detail
            );
            throw new Error('Login failed: ' + r.status);
        }

        const d = await r.json();
        this.token = d.token;
        localStorage.setItem('gs_token', d.token);
        return d;
    },

    logout() {
        this.token = null;
        localStorage.removeItem('gs_token');
        localStorage.removeItem('gs_email');
        Router.navigate('/login');
    },

    isAuth() { return !!this.token; }
};
