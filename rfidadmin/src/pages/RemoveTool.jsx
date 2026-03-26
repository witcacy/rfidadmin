import React, { useState, useEffect } from 'react';
import { reasonsApi, toolsApi } from '../api/apiClient';

export default function RemoveTool() {
    const [reasons, setReasons] = useState([]);
    const [form, setForm] = useState({ reasonForRequestId: '', rfidTag: '' });
    const [toolInfo, setToolInfo] = useState(null);
    const [message, setMessage] = useState(null);

    useEffect(() => {
        reasonsApi.getAll()
            .then(setReasons)
            .catch(() => setMessage({ type: 'danger', text: 'Error loading reasons.' }));
    }, []);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleLookup = async () => {
        if (!form.rfidTag) return;
        try {
            const tool = await toolsApi.getByRfidTag(form.rfidTag);
            setToolInfo(tool);
        } catch {
            setToolInfo(null);
        }
    };

    const handleRemove = async () => {
        setMessage(null);
        if (!toolInfo) {
            setMessage({ type: 'warning', text: 'No tool found for this RFID tag.' });
            return;
        }
        try {
            await toolsApi.remove({
                toolId: toolInfo.id,
                reasonForRequestId: Number(form.reasonForRequestId),
                rfidTag: form.rfidTag,
            });
            setMessage({ type: 'success', text: 'Tool removed successfully.' });
            setForm({ reasonForRequestId: '', rfidTag: '' });
            setToolInfo(null);
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error removing tool.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Remove Tool</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form className="p-3 border rounded bg-light">
                <div className="mb-3">
                    <label className="form-label">Reason for Request</label>
                    <select name="reasonForRequestId" className="form-select" value={form.reasonForRequestId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {reasons.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">RFID Read</label>
                    <input type="text" name="rfidTag" className="form-control" value={form.rfidTag} onChange={handleChange} onBlur={handleLookup} />
                </div>
                {toolInfo && (
                    <div className="alert alert-info">
                        Tool found: {toolInfo.serialNumber} &mdash; {toolInfo.description}
                    </div>
                )}
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-danger" onClick={handleRemove}>
                        <i className="bi bi-trash me-2"></i> Remove Tool
                    </button>
                </div>
            </form>
        </div>
    );
}