import React, { useState, useEffect } from 'react';
import { reasonsApi, areasApi, toolTypesApi, ticketsApi } from '../api/apiClient';

export default function RequestTool() {
    const [reasons, setReasons] = useState([]);
    const [areas, setAreas] = useState([]);
    const [toolTypes, setToolTypes] = useState([]);
    const [form, setForm] = useState({ reasonForRequestId: '', areaId: '', toolTypeId: '' });
    const [message, setMessage] = useState(null);

    useEffect(() => {
        Promise.all([reasonsApi.getAll(), areasApi.getAll(), toolTypesApi.getAll()])
            .then(([r, a, t]) => { setReasons(r); setAreas(a); setToolTypes(t); })
            .catch(() => setMessage({ type: 'danger', text: 'Error loading catalogs.' }));
    }, []);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleSubmit = async (newTicket = false) => {
        setMessage(null);
        try {
            await ticketsApi.createRequestTool({
                reasonForRequestId: Number(form.reasonForRequestId),
                areaId: Number(form.areaId),
                toolTypeId: Number(form.toolTypeId),
                createdByUserId: 1, // TODO: replace with authenticated user
            });
            setMessage({ type: 'success', text: 'Ticket submitted successfully.' });
            if (newTicket) setForm({ reasonForRequestId: '', areaId: '', toolTypeId: '' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error submitting ticket.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Request Tool</h4>
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
                    <label className="form-label">Area</label>
                    <select name="areaId" className="form-select" value={form.areaId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Tool Type</label>
                    <select name="toolTypeId" className="form-select" value={form.toolTypeId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {toolTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                    </select>
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-primary" onClick={() => handleSubmit(false)}>
                        <i className="bi bi-send me-2"></i> Submit Ticket
                    </button>
                    <button type="button" className="btn btn-secondary" onClick={() => handleSubmit(true)}>
                        <i className="bi bi-plus-circle me-2"></i> New Ticket
                    </button>
                </div>
            </form>
        </div>
    );
}