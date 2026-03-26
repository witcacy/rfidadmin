import React, { useState, useEffect } from 'react';
import { toolTypesApi, toolsApi } from '../api/apiClient';

export default function NewTool() {
    const [toolTypes, setToolTypes] = useState([]);
    const [form, setForm] = useState({ toolTypeId: '', serialNumber: '', description: '', rfidTag: '' });
    const [message, setMessage] = useState(null);

    useEffect(() => {
        toolTypesApi.getAll()
            .then(setToolTypes)
            .catch(() => setMessage({ type: 'danger', text: 'Error loading tool types.' }));
    }, []);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleSubmit = async (addAnother = false) => {
        setMessage(null);
        try {
            await toolsApi.create({
                toolTypeId: Number(form.toolTypeId),
                serialNumber: form.serialNumber,
                description: form.description,
                rfidTag: form.rfidTag,
            });
            setMessage({ type: 'success', text: 'Tool created successfully.' });
            if (addAnother) setForm({ toolTypeId: '', serialNumber: '', description: '', rfidTag: '' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error creating tool.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Add New Tool</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form>
                <div className="mb-3">
                    <label className="form-label">Tool Type</label>
                    <select name="toolTypeId" className="form-select" value={form.toolTypeId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {toolTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Serial Number</label>
                    <input type="text" name="serialNumber" className="form-control" value={form.serialNumber} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Description</label>
                    <input type="text" name="description" className="form-control" value={form.description} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">RFID Read</label>
                    <input type="text" name="rfidTag" className="form-control" value={form.rfidTag} onChange={handleChange} />
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-success" onClick={() => handleSubmit(false)}>
                        <i className="bi bi-plus-circle me-2"></i> Add Tool
                    </button>
                    <button type="button" className="btn btn-secondary" onClick={() => handleSubmit(true)}>
                        <i className="bi bi-plus-square me-2"></i> Add Another Tool
                    </button>
                </div>
            </form>
        </div>
    );
}