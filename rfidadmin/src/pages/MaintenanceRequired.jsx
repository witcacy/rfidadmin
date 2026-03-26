import React, { useState, useEffect } from 'react';
import { reasonsApi, toolTypesApi, areasApi, ticketsApi } from '../api/apiClient';

export default function MaintenanceRequired() {
    const [reasons, setReasons] = useState([]);
    const [toolTypes, setToolTypes] = useState([]);
    const [areas, setAreas] = useState([]);
    const [form, setForm] = useState({ reasonForRequestId: '', toolTypeId: '', areaId: '' });
    const [message, setMessage] = useState(null);

    useEffect(() => {
        Promise.all([reasonsApi.getAll(), toolTypesApi.getAll(), areasApi.getAll()])
            .then(([r, t, a]) => { setReasons(r); setToolTypes(t); setAreas(a); })
            .catch(() => setMessage({ type: 'danger', text: 'Error loading catalogs.' }));
    }, []);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleSubmit = async () => {
        setMessage(null);
        try {
            await ticketsApi.createMaintenance({
                reasonForRequestId: Number(form.reasonForRequestId),
                toolTypeId: Number(form.toolTypeId),
                areaId: Number(form.areaId),
                createdByUserId: 1, // TODO: replace with authenticated user
            });
            setMessage({ type: 'success', text: 'Maintenance ticket submitted successfully.' });
            setForm({ reasonForRequestId: '', toolTypeId: '', areaId: '' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error submitting ticket.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Maintenance Required</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form>
                <div className="mb-3">
                    <label className="form-label">Reason for Request</label>
                    <select name="reasonForRequestId" className="form-select" value={form.reasonForRequestId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {reasons.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Tool Type</label>
                    <select name="toolTypeId" className="form-select" value={form.toolTypeId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {toolTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Area</label>
                    <select name="areaId" className="form-select" value={form.areaId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                    </select>
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-warning" onClick={handleSubmit}>
                        <i className="bi bi-tools me-2"></i> Submit Ticket
                    </button>
                </div>
            </form>
        </div>
    );
}