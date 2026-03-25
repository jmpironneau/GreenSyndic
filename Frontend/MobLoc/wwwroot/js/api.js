// MobSyndic — API client pointing to backend
const API_URL = 'http://localhost:5050';

const API = {
    token: localStorage.getItem('gs_token'),

    async request(method, path, body) {
        const opts = {
            method,
            headers: { 'Content-Type': 'application/json' }
        };
        if (this.token) opts.headers['Authorization'] = `Bearer ${this.token}`;
        if (body) opts.body = JSON.stringify(body);

        const resp = await fetch(`${API_URL}${path}`, opts);
        if (resp.status === 401) { this.logout(); return null; }
        if (!resp.ok) throw new Error(`API ${resp.status}`);
        const text = await resp.text();
        return text ? JSON.parse(text) : null;
    },

    get: (path) => API.request('GET', path),
    post: (path, body) => API.request('POST', path, body),

    logout() {
        localStorage.removeItem('gs_token');
        this.token = null;
        Router.navigate('/');
    },

    async login(email, password) {
        const resp = await fetch(`${API_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        if (!resp.ok) return false;
        const data = await resp.json();
        this.token = data.token;
        localStorage.setItem('gs_token', data.token);
        return true;
    }
};
