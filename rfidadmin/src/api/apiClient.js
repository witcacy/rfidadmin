const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7070/api';

async function request(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const config = {
        headers: { 'Content-Type': 'application/json' },
        ...options,
    };

    const response = await fetch(url, config);

    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || `HTTP ${response.status}`);
    }

    if (response.status === 204) return null;
    return response.json();
}

export const api = {
    get: (endpoint) => request(endpoint),
    post: (endpoint, body) => request(endpoint, { method: 'POST', body: JSON.stringify(body) }),
    put: (endpoint, body) => request(endpoint, { method: 'PUT', body: JSON.stringify(body) }),
    patch: (endpoint, body) => request(endpoint, { method: 'PATCH', body: body ? JSON.stringify(body) : undefined }),
    delete: (endpoint) => request(endpoint, { method: 'DELETE' }),
};


export const areasApi = {
    getAll: () => api.get('/areas'),
    getById: (id) => api.get(`/areas/${id}`),
    create: (name) => api.post('/areas', { name }),
    delete: (id) => api.delete(`/areas/${id}`),
};

export const toolTypesApi = {
    getAll: () => api.get('/tooltypes'),
    getById: (id) => api.get(`/tooltypes/${id}`),
    create: (name) => api.post('/tooltypes', { name }),
    delete: (id) => api.delete(`/tooltypes/${id}`),
};

export const reasonsApi = {
    getAll: () => api.get('/reasonsforrequest'),
    getById: (id) => api.get(`/reasonsforrequest/${id}`),
    create: (name) => api.post('/reasonsforrequest', { name }),
    delete: (id) => api.delete(`/reasonsforrequest/${id}`),
};

// --- Roles ---
export const rolesApi = {
    getAll: () => api.get('/roles'),
    getById: (id) => api.get(`/roles/${id}`),
    getWithPermissions: (id) => api.get(`/roles/${id}/permissions`),
};

// --- Users ---
export const usersApi = {
    getAll: () => api.get('/users'),
    getById: (id) => api.get(`/users/${id}`),
    getByBadgeId: (badgeId) => api.get(`/users/badge/${badgeId}`),
    create: (data) => api.post('/users', data),
    update: (id, data) => api.put(`/users/${id}`, data),
    deactivate: (id) => api.patch(`/users/${id}/deactivate`),
};

// --- Tools ---
export const toolsApi = {
    getAll: () => api.get('/tools'),
    getById: (id) => api.get(`/tools/${id}`),
    getByRfidTag: (rfidTag) => api.get(`/tools/rfid/${rfidTag}`),
    getByStatus: (status) => api.get(`/tools/status/${status}`),
    create: (data) => api.post('/tools', data),
    remove: (data) => api.post('/tools/remove', data),
};

// --- Tickets ---
export const ticketsApi = {
    getAll: () => api.get('/tickets'),
    getOpen: () => api.get('/tickets/open'),
    getById: (id) => api.get(`/tickets/${id}`),
    getByStatus: (status) => api.get(`/tickets/status/${status}`),
    getReport: (startDate, endDate, status) =>
        api.get(`/tickets/report?startDate=${startDate}&endDate=${endDate}${status ? `&status=${status}` : ''}`),
    createRequestTool: (data) => api.post('/tickets/request-tool', data),
    createMaintenance: (data) => api.post('/tickets/maintenance', data),
    close: (id) => api.patch(`/tickets/${id}/close`),
};

// --- Tool Assignments ---
export const toolAssignmentsApi = {
    getActiveByUser: (userId) => api.get(`/toolassignments/user/${userId}`),
    assign: (data) => api.post('/toolassignments', data),
    returnTool: (id) => api.patch(`/toolassignments/${id}/return`),
};

// --- RFID Scans ---
export const rfidScansApi = {
    getByTagId: (tagId) => api.get(`/rfidscans/tag/${tagId}`),
    getByDateRange: (start, end) => api.get(`/rfidscans/range?start=${start}&end=${end}`),
    recordScan: (data) => api.post('/rfidscans', data),
};
