import React, { useState } from 'react';
import { rfidScansApi } from '../api/apiClient';

export default function RFIDScan() {
    const [tagId, setTagId] = useState('');
    const [records, setRecords] = useState([]);
    const [message, setMessage] = useState(null);

    const handleSearch = async () => {
        setMessage(null);
        if (!tagId) return;
        try {
            const data = await rfidScansApi.getByTagId(tagId);
            setRecords(data);
            if (data.length === 0) setMessage({ type: 'info', text: 'No scan records found for this tag.' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error searching scans.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">RFID Scan</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <div className="input-group mb-3">
                <input type="text" className="form-control" placeholder="Enter RFID Tag ID" value={tagId} onChange={(e) => setTagId(e.target.value)} />
                <button className="btn btn-primary" type="button" onClick={handleSearch}>
                    <i className="bi bi-search me-2"></i> Search
                </button>
            </div>

            {records.length > 0 && (
                <table className="table table-striped">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Tag ID</th>
                            <th>Antenna</th>
                            <th>Scanned At</th>
                        </tr>
                    </thead>
                    <tbody>
                        {records.map(r => (
                            <tr key={r.id}>
                                <td>{r.id}</td>
                                <td>{r.tagId}</td>
                                <td>{r.antennaId || 'N/A'}</td>
                                <td>{new Date(r.scannedAt).toLocaleString()}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            {records.length === 0 && !message && (
                <div className="alert alert-info">
                    <i className="bi bi-upc-scan me-2"></i> No tag detected yet.
                </div>
            )}
        </div>
    );
}