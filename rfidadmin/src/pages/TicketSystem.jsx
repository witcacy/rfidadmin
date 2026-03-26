import React, { useState, useEffect } from 'react';
import { ticketsApi } from '../api/apiClient';

export default function TicketSystem() {
    const [tickets, setTickets] = useState([]);
    const [message, setMessage] = useState(null);

    const loadTickets = async () => {
        try {
            const data = await ticketsApi.getOpen();
            setTickets(data);
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error loading tickets.' });
        }
    };

    useEffect(() => { loadTickets(); }, []);

    const handleClose = async (id) => {
        setMessage(null);
        try {
            await ticketsApi.close(id);
            setMessage({ type: 'success', text: `Ticket #${id} closed.` });
            loadTickets();
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error closing ticket.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Ticket System</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

            {tickets.length === 0 ? (
                <div className="alert alert-info">No open tickets.</div>
            ) : (
                <table className="table table-striped">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Type</th>
                            <th>Status</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {tickets.map(t => (
                            <tr key={t.id}>
                                <td>{t.id}</td>
                                <td>{t.type}</td>
                                <td>{t.status}</td>
                                <td>{new Date(t.createdAt).toLocaleDateString()}</td>
                                <td>
                                    <button className="btn btn-sm btn-outline-danger" onClick={() => handleClose(t.id)}>
                                        <i className="bi bi-x-circle me-1"></i> Close
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );
}