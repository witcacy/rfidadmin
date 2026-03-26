
import React, { useState } from 'react';
import { toolAssignmentsApi } from '../api/apiClient';

export default function AssignTool() {
    const [form, setForm] = useState({ badgeId: '', rfidTag: '', ticketId: '' });
    const [message, setMessage] = useState(null);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleAssign = async () => {
        setMessage(null);
        try {
            await toolAssignmentsApi.assign({
                badgeId: form.badgeId,
                rfidTag: form.rfidTag,
                ticketId: form.ticketId ? Number(form.ticketId) : null,
            });
            setMessage({ type: 'success', text: 'Tool assigned successfully.' });
            setForm({ badgeId: '', rfidTag: '', ticketId: '' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error assigning tool.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Assign Tool</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form>
                <div className="mb-3">
                    <label className="form-label">Scan Badge</label>
                    <input type="text" name="badgeId" className="form-control" placeholder="Enter badge ID" value={form.badgeId} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">RFID Read</label>
                    <input type="text" name="rfidTag" className="form-control" placeholder="Waiting for RFID..." value={form.rfidTag} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Ticket ID (optional)</label>
                    <input type="text" name="ticketId" className="form-control" placeholder="Enter ticket ID" value={form.ticketId} onChange={handleChange} />
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-success" onClick={handleAssign}>
                        <i className="bi bi-person-check me-2"></i> Assign Tool
                    </button>
                </div>
            </form>
        </div>
    );
}