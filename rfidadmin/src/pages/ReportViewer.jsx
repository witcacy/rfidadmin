

import React, { useState } from 'react';
import { ticketsApi } from '../api/apiClient';

export default function ReportViewer() {
    const [form, setForm] = useState({ startDate: '', endDate: '', status: 'All' });
    const [results, setResults] = useState([]);
    const [message, setMessage] = useState(null);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleRunReport = async () => {
        setMessage(null);
        try {
            const data = await ticketsApi.getReport(form.startDate, form.endDate, form.status);
            setResults(data);
            if (data.length === 0) setMessage({ type: 'info', text: 'No results found.' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error running report.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Report Viewer</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form>
                <div className="mb-3">
                    <label className="form-label">Start Date</label>
                    <input type="date" name="startDate" className="form-control" value={form.startDate} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">End Date</label>
                    <input type="date" name="endDate" className="form-control" value={form.endDate} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Status</label>
                    <select name="status" className="form-select" value={form.status} onChange={handleChange}>
                        <option value="All">All</option>
                        <option value="Open">Open</option>
                        <option value="Closed">Closed</option>
                    </select>
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-primary" onClick={handleRunReport}>
                        <i className="bi bi-bar-chart-line me-2"></i> Run Report
                    </button>
                </div>
            </form>

            {results.length > 0 && (
                <table className="table table-striped mt-3">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Type</th>
                            <th>Status</th>
                            <th>Created</th>
                        </tr>
                    </thead>
                    <tbody>
                        {results.map(t => (
                            <tr key={t.id}>
                                <td>{t.id}</td>
                                <td>{t.type}</td>
                                <td>{t.status}</td>
                                <td>{new Date(t.createdAt).toLocaleDateString()}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );
}